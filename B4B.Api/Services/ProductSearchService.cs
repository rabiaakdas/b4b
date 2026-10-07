using System.Diagnostics;
using System.Text;
using System.Text.Json;
using B4B.Api.Data;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace B4B.Api.Services;

public class ProductSearchService
{
    private const int MaxPageSize = 100;
    private const int MaxResultWindow = 10000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ElasticsearchClient _client;
    private readonly AppDbContext _dbContext;
    private readonly ElasticsearchOptions _options;
    private readonly RedisOptions _redisOptions;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ProductSearchService> _logger;

    public ProductSearchService(
        ElasticsearchClient client,
        AppDbContext dbContext,
        IOptions<ElasticsearchOptions> options,
        IOptions<RedisOptions> redisOptions,
        IConnectionMultiplexer redis,
        ILogger<ProductSearchService> logger)
    {
        _client = client;
        _dbContext = dbContext;
        _options = options.Value;
        _redisOptions = redisOptions.Value;
        _redis = redis;
        _logger = logger;
    }

    public async Task EnsureProductIndexAsync(CancellationToken cancellationToken = default)
    {
        var exists = await _client.Transport.RequestAsync<ElasticsearchStringResponse>(
            Elastic.Transport.HttpMethod.GET,
            $"/{_options.ProductIndex}",
            cancellationToken);

        if (exists.IsValidResponse)
        {
            return;
        }

        var mapping = new
        {
            settings = new
            {
                analysis = new
                {
                    normalizer = new
                    {
                        code_normalizer = new
                        {
                            type = "custom",
                            filter = new[] { "lowercase", "asciifolding" }
                        }
                    }
                }
            },
            mappings = new
            {
                properties = new
                {
                    ProductId = new { type = "keyword" },
                    CompanyId = new { type = "keyword" },
                    ProductCode = new { type = "keyword", normalizer = "code_normalizer" },
                    ProductName = new { type = "text", fields = new { keyword = new { type = "keyword" } } },
                    Price = new { type = "scaled_float", scaling_factor = 100 }
                }
            }
        };

        var response = await _client.Transport.RequestAsync<ElasticsearchStringResponse>(
            Elastic.Transport.HttpMethod.PUT,
            $"/{_options.ProductIndex}",
            PostData.String(JsonSerializer.Serialize(mapping)),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException("Elasticsearch ürün indeksi oluşturulamadı.");
        }
    }

    public async Task<ProductIndexResult> ReindexProductsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureProductIndexAsync(cancellationToken);

        var indexedCount = 0;
        var errorCount = 0;
        const int batchSize = 1000;

        for (var skip = 0; ; skip += batchSize)
        {
            var documents = await _dbContext.Products
                .AsNoTracking()
                .OrderBy(product => product.Id)
                .Skip(skip)
                .Take(batchSize)
                .Select(product => new ProductSearchDocument
                {
                    ProductId = product.Id,
                    CompanyId = product.CompanyId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    Price = product.Price
                })
                .ToListAsync(cancellationToken);

            if (documents.Count == 0)
            {
                break;
            }

            var result = await BulkIndexProductsAsync(documents, refresh: false, cancellationToken);
            indexedCount += result.IndexedCount;
            errorCount += result.ErrorCount;
        }

        await RefreshProductIndexAsync(cancellationToken);

        if (errorCount == 0)
        {
            await IncrementProductSearchCacheVersionAsync(cancellationToken);
        }

        return new ProductIndexResult(indexedCount, errorCount);
    }

    public async Task<ProductIndexResult> BulkIndexProductsAsync(
        IReadOnlyCollection<ProductSearchDocument> documents,
        bool refresh,
        CancellationToken cancellationToken = default)
    {
        if (documents.Count == 0)
        {
            return new ProductIndexResult(0, 0);
        }

        await EnsureProductIndexAsync(cancellationToken);

        var bulkPayload = BuildBulkPayload(documents);
        var refreshQuery = refresh ? "?refresh=true" : string.Empty;
        var response = await _client.Transport.RequestAsync<ElasticsearchStringResponse>(
            Elastic.Transport.HttpMethod.POST,
            $"/{_options.ProductIndex}/_bulk{refreshQuery}",
            PostData.String(bulkPayload),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            return new ProductIndexResult(0, documents.Count);
        }

        var bulkErrors = CountBulkErrors(response.Body);
        return new ProductIndexResult(documents.Count - bulkErrors, bulkErrors);
    }

    public async Task RefreshProductIndexAsync(CancellationToken cancellationToken = default)
    {
        var response = await _client.Transport.RequestAsync<ElasticsearchStringResponse>(
            Elastic.Transport.HttpMethod.POST,
            $"/{_options.ProductIndex}/_refresh",
            cancellationToken);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException("Elasticsearch ürün indeksi yenilenemedi.");
        }
    }

    public async Task<long> CountProductsInIndexAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            query = new
            {
                term = new
                {
                    CompanyId = new
                    {
                        value = companyId.ToString()
                    }
                }
            }
        };

        var response = await _client.Transport.RequestAsync<ElasticsearchStringResponse>(
            Elastic.Transport.HttpMethod.POST,
            $"/{_options.ProductIndex}/_count",
            PostData.String(JsonSerializer.Serialize(request)),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException("Elasticsearch ürün sayısı alınamadı.");
        }

        using var json = JsonDocument.Parse(response.Body);
        return json.RootElement.GetProperty("count").GetInt64();
    }

    public async Task IncrementSearchCacheVersionAsync(CancellationToken cancellationToken = default)
    {
        await IncrementProductSearchCacheVersionAsync(cancellationToken);
    }

    public async Task<ProductIndexResult> IndexProductsByCodePrefixAsync(
        string productCodePrefix,
        CancellationToken cancellationToken = default)
    {
        await EnsureProductIndexAsync(cancellationToken);

        var indexedCount = 0;
        var errorCount = 0;
        const int batchSize = 1000;

        for (var skip = 0; ; skip += batchSize)
        {
            var documents = await _dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Id)
                .Where(product => product.ProductCode.StartsWith(productCodePrefix))
                .Skip(skip)
                .Take(batchSize)
            .Select(product => new ProductSearchDocument
            {
                ProductId = product.Id,
                CompanyId = product.CompanyId,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Price = product.Price
            })
            .ToListAsync(cancellationToken);

            if (documents.Count == 0)
            {
                break;
            }

            var result = await BulkIndexProductsAsync(documents, refresh: false, cancellationToken);
            indexedCount += result.IndexedCount;
            errorCount += result.ErrorCount;
        }

        await RefreshProductIndexAsync(cancellationToken);

        if (errorCount == 0)
        {
            await IncrementProductSearchCacheVersionAsync(cancellationToken);
        }

        return new ProductIndexResult(indexedCount, errorCount);
    }

    public async Task<ProductSearchResult> SearchProductsAsync(
        Guid companyId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        if (((page - 1) * pageSize) + pageSize > MaxResultWindow)
        {
            throw new ProductSearchPageTooDeepException(
                $"Bu örnekte en fazla {MaxResultWindow} sonuca kadar sayfalama yapılabilir. Lütfen arama metnini daraltın.");
        }

        var durationStopwatch = Stopwatch.StartNew();
        var normalizedSearch = NormalizeSearch(search);
        var cacheVersion = await GetProductSearchCacheVersionAsync(cancellationToken);
        var cacheKey = BuildCacheKey(cacheVersion, companyId, normalizedSearch, page, pageSize);
        var cachedResult = await TryGetCachedSearchResultAsync(cacheKey, cancellationToken);
        if (cachedResult is not null)
        {
            durationStopwatch.Stop();
            return cachedResult with
            {
                ElasticsearchElapsedMilliseconds = 0,
                DurationMilliseconds = durationStopwatch.ElapsedMilliseconds,
                CacheStatus = "HIT"
            };
        }

        var request = BuildSearchRequest(companyId, search, page, pageSize);
        var stopwatch = Stopwatch.StartNew();

        var response = await _client.Transport.RequestAsync<ElasticsearchStringResponse>(
            Elastic.Transport.HttpMethod.POST,
            $"/{_options.ProductIndex}/_search",
            PostData.String(JsonSerializer.Serialize(request)),
            cancellationToken);

        stopwatch.Stop();

        if (!response.IsValidResponse)
        {
            throw new ProductSearchUnavailableException("Elasticsearch servisine ulaşılamıyor. Lütfen daha sonra tekrar deneyin.");
        }

        var result = ParseSearchResponse(response.Body, stopwatch.ElapsedMilliseconds, page, pageSize);
        durationStopwatch.Stop();

        result = result with
        {
            DurationMilliseconds = durationStopwatch.ElapsedMilliseconds,
            CacheStatus = "MISS"
        };

        await TrySetCachedSearchResultAsync(cacheKey, result, cancellationToken);

        return result;
    }

    private async Task<long> GetProductSearchCacheVersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var value = await _redis.GetDatabase().StringGetAsync(
                ProductSearchCacheVersionKey,
                CommandFlags.DemandMaster).WaitAsync(cancellationToken);

            return long.TryParse(value.ToString(), out var version) && version > 0 ? version : 1;
        }
        catch (Exception exception) when (IsRedisException(exception))
        {
            _logger.LogWarning(exception, "Redis ürün arama cache sürümü okunamadı. Elasticsearch araması cache kullanılmadan devam edecek.");
            return 1;
        }
    }

    private async Task IncrementProductSearchCacheVersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _redis.GetDatabase().StringIncrementAsync(ProductSearchCacheVersionKey)
                .WaitAsync(cancellationToken);
        }
        catch (Exception exception) when (IsRedisException(exception))
        {
            _logger.LogWarning(exception, "Redis ürün arama cache sürümü güncellenemedi.");
        }
    }

    private async Task<ProductSearchResult?> TryGetCachedSearchResultAsync(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            var value = await _redis.GetDatabase().StringGetAsync(cacheKey).WaitAsync(cancellationToken);
            if (value.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<ProductSearchResult>(value.ToString(), JsonOptions);
        }
        catch (Exception exception) when (IsRedisException(exception))
        {
            _logger.LogWarning(exception, "Redis ürün arama cache okuması başarısız oldu. Elasticsearch aramasıyla devam edilecek.");
            return null;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Redis ürün arama cache verisi okunamadı. Elasticsearch aramasıyla devam edilecek.");
            return null;
        }
    }

    private async Task TrySetCachedSearchResultAsync(string cacheKey, ProductSearchResult result, CancellationToken cancellationToken)
    {
        try
        {
            var ttlSeconds = Math.Max(_redisOptions.ProductSearchCacheSeconds, 1);
            var cacheValue = JsonSerializer.Serialize(result with
            {
                CacheStatus = "MISS",
                DurationMilliseconds = 0
            }, JsonOptions);

            await _redis.GetDatabase()
                .StringSetAsync(cacheKey, cacheValue, TimeSpan.FromSeconds(ttlSeconds))
                .WaitAsync(cancellationToken);
        }
        catch (Exception exception) when (IsRedisException(exception))
        {
            _logger.LogWarning(exception, "Redis ürün arama cache yazması başarısız oldu.");
        }
    }

    private static string BuildBulkPayload(IEnumerable<ProductSearchDocument> documents)
    {
        var builder = new StringBuilder();

        foreach (var document in documents)
        {
            var action = new
            {
                index = new
                {
                    _id = document.ProductId.ToString()
                }
            };

            builder.AppendLine(JsonSerializer.Serialize(action, JsonOptions));
            builder.AppendLine(JsonSerializer.Serialize(document, JsonOptions));
        }

        return builder.ToString();
    }

    private static int CountBulkErrors(string body)
    {
        using var json = JsonDocument.Parse(body);
        if (!json.RootElement.TryGetProperty("errors", out var errors) || !errors.GetBoolean())
        {
            return 0;
        }

        var errorCount = 0;
        foreach (var item in json.RootElement.GetProperty("items").EnumerateArray())
        {
            var index = item.GetProperty("index");
            if (index.TryGetProperty("error", out _))
            {
                errorCount++;
            }
        }

        return errorCount;
    }

    private const string ProductSearchCacheVersionKey = "b4b:products:search:version:v3";

    private static object BuildSearchRequest(Guid companyId, string? search, int page, int pageSize)
    {
        var normalizedSearch = NormalizeSearch(search);
        var filter = new object[]
        {
            new
            {
                term = new
                {
                    CompanyId = new
                    {
                        value = companyId.ToString()
                    }
                }
            }
        };

        object query = string.IsNullOrWhiteSpace(normalizedSearch)
            ? new
            {
                @bool = new
                {
                    filter,
                    must = new object[] { new { match_all = new { } } }
                }
            }
            : IsCodeLikeSearch(normalizedSearch)
                ? new
                {
                    @bool = new
                    {
                        filter,
                        must = new object[]
                        {
                            new
                            {
                                wildcard = new
                                {
                                    ProductCode = new
                                    {
                                        value = $"*{normalizedSearch}*"
                                    }
                                }
                            }
                        }
                    }
                }
            : new
            {
                @bool = new
                {
                    filter,
                    should = new object[]
                    {
                        new
                        {
                            match = new
                            {
                                ProductName = new
                                {
                                    query = normalizedSearch,
                                    @operator = "and",
                                    fuzziness = "AUTO"
                                }
                            }
                        },
                        new
                        {
                            wildcard = new
                            {
                                ProductCode = new
                                {
                                    value = $"*{normalizedSearch}*"
                                }
                            }
                        }
                    },
                    minimum_should_match = 1
                }
            };

        return new
        {
            from = (page - 1) * pageSize,
            size = pageSize,
            track_total_hits = true,
            sort = new object[]
            {
                new Dictionary<string, object>
                {
                    ["ProductCode"] = new { order = "asc" }
                }
            },
            query
        };
    }

    private static string NormalizeSearch(string? search)
    {
        return search?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static bool IsCodeLikeSearch(string normalizedSearch)
    {
        return normalizedSearch.Any(char.IsDigit) || normalizedSearch.Contains('-');
    }

    private static string BuildCacheKey(long version, Guid companyId, string normalizedSearch, int page, int pageSize)
    {
        var encodedSearch = Convert.ToBase64String(Encoding.UTF8.GetBytes(normalizedSearch));
        return $"b4b:products:search:v{version}:company:{companyId}:q:{encodedSearch}:page:{page}:size:{pageSize}";
    }

    private static bool IsRedisException(Exception exception)
    {
        return exception is RedisException ||
               exception is TimeoutException ||
               exception is ObjectDisposedException;
    }

    private static ProductSearchResult ParseSearchResponse(string body, long elapsedMilliseconds, int page, int pageSize)
    {
        using var json = JsonDocument.Parse(body);
        var hitsElement = json.RootElement.GetProperty("hits");
        var totalCount = hitsElement.GetProperty("total").GetProperty("value").GetInt64();
        var documents = new List<ProductSearchDocument>();

        foreach (var hit in hitsElement.GetProperty("hits").EnumerateArray())
        {
            var source = hit.GetProperty("_source");
            var document = source.Deserialize<ProductSearchDocument>(JsonOptions);
            if (document is not null)
            {
                documents.Add(document);
            }
        }

        return new ProductSearchResult(documents, totalCount, elapsedMilliseconds, page, pageSize, 0, "MISS");
    }
}

public record ProductIndexResult(int IndexedCount, int ErrorCount);

public record ProductSearchResult(
    List<ProductSearchDocument> Products,
    long TotalCount,
    long ElasticsearchElapsedMilliseconds,
    int Page,
    int PageSize,
    long DurationMilliseconds,
    string CacheStatus);

public class ProductSearchUnavailableException : Exception
{
    public ProductSearchUnavailableException(string message)
        : base(message)
    {
    }
}

public class ProductSearchPageTooDeepException : Exception
{
    public ProductSearchPageTooDeepException(string message)
        : base(message)
    {
    }
}
