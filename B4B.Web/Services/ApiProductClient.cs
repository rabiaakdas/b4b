using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace B4B.Web.Services;

public class ApiProductClient
{
    private readonly HttpClient _httpClient;

    public ApiProductClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductListResult> GetProductsAsync(string token, string? search, int page, int pageSize)
    {
        var query = new List<string>
        {
            $"sayfa={page}",
            $"sayfaBoyutu={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"arama={Uri.EscapeDataString(search.Trim())}");
        }

        var url = $"api/urunler?{string.Join("&", query)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return ProductListResult.CreateUnauthorized();
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            return ProductListResult.CreateFailed(error?.Message ?? "Ürün araması şu anda yapılamıyor.");
        }

        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        return ProductListResult.CreateSuccess(
            products ?? [],
            ReadLongHeader(response, "X-Total-Count"),
            ReadLongHeader(response, "X-Search-Took-Ms"),
            ReadLongHeader(response, "X-Search-Duration-Ms"),
            ReadIntHeader(response, "X-Page"),
            ReadIntHeader(response, "X-Page-Size"),
            response.Headers.TryGetValues("X-Cache", out var cacheValues) ? cacheValues.FirstOrDefault() : null);
    }

    private static long ReadLongHeader(HttpResponseMessage response, string name)
    {
        return response.Headers.TryGetValues(name, out var values) &&
               long.TryParse(values.FirstOrDefault(), out var value)
            ? value
            : 0;
    }

    private static int ReadIntHeader(HttpResponseMessage response, string name)
    {
        return response.Headers.TryGetValues(name, out var values) &&
               int.TryParse(values.FirstOrDefault(), out var value)
            ? value
            : 0;
    }
}

public class ProductListResult
{
    private ProductListResult(
        bool success,
        bool unauthorized,
        List<ProductDto> products,
        string? errorMessage,
        long totalCount,
        long elasticsearchTookMilliseconds,
        long searchDurationMilliseconds,
        int page,
        int pageSize,
        string? cacheStatus)
    {
        Success = success;
        Unauthorized = unauthorized;
        Products = products;
        ErrorMessage = errorMessage;
        TotalCount = totalCount;
        ElasticsearchTookMilliseconds = elasticsearchTookMilliseconds;
        SearchDurationMilliseconds = searchDurationMilliseconds;
        Page = page;
        PageSize = pageSize;
        CacheStatus = cacheStatus;
    }

    public bool Success { get; }

    public bool Unauthorized { get; }

    public List<ProductDto> Products { get; }

    public string? ErrorMessage { get; }

    public long TotalCount { get; }

    public long ElasticsearchTookMilliseconds { get; }

    public long SearchDurationMilliseconds { get; }

    public int Page { get; }

    public int PageSize { get; }

    public string? CacheStatus { get; }

    public static ProductListResult CreateSuccess(
        List<ProductDto> products,
        long totalCount,
        long elasticsearchTookMilliseconds,
        long searchDurationMilliseconds,
        int page,
        int pageSize,
        string? cacheStatus) =>
        new(true, false, products, null, totalCount, elasticsearchTookMilliseconds, searchDurationMilliseconds, page, pageSize, cacheStatus);

    public static ProductListResult CreateFailed(string errorMessage) => new(false, false, [], errorMessage, 0, 0, 0, 0, 0, null);

    public static ProductListResult CreateUnauthorized() => new(false, true, [], null, 0, 0, 0, 0, 0, null);
}

public record ProductDto(
    Guid Id,
    [property: JsonPropertyName("urunKodu")] string ProductCode,
    [property: JsonPropertyName("urunAdi")] string ProductName,
    [property: JsonPropertyName("fiyat")] decimal Price);

public record ApiErrorResponse(
    [property: JsonPropertyName("message")] string Message);

