using System.Diagnostics;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using B4B.Api.Data;
using B4B.Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace B4B.Api.Services;

public class TestProductDataSeeder
{
    public const string ProductCodePrefix = "B4BTEST";

    private static readonly string[] ProductTypes =
    [
        "Akıllı Telefon",
        "Dizüstü Bilgisayar",
        "Kablosuz Kulaklık",
        "Mekanik Klavye",
        "Oyuncu Mouse",
        "Monitör",
        "Tablet",
        "Harici Disk",
        "Yazıcı",
        "Projeksiyon",
        "Router",
        "Güç Kaynağı",
        "Web Kamera",
        "Mikrofon",
        "Taşınabilir Şarj Cihazı",
        "Akıllı Saat"
    ];

    private static readonly string[] Qualities =
    [
        "Ekonomik",
        "Profesyonel",
        "Kurumsal",
        "Hızlı",
        "Dayanıklı",
        "Kompakt",
        "Sessiz",
        "Yüksek Performanslı"
    ];

    private readonly AppDbContext _dbContext;
    private readonly ProductSearchService _productSearchService;
    private readonly IConfiguration _configuration;

    public TestProductDataSeeder(
        AppDbContext dbContext,
        ProductSearchService productSearchService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _productSearchService = productSearchService;
        _configuration = configuration;
    }

    public async Task<TestProductSeedResult> SeedAsync(
        int totalCount,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (totalCount <= 0)
        {
            throw new ArgumentException("Ürün sayısı pozitif olmalıdır.", nameof(totalCount));
        }

        batchSize = Math.Clamp(batchSize, 100, 5000);
        var companyAId = new Guid("11111111-1111-1111-1111-111111111111");
        var companyBId = new Guid("22222222-2222-2222-2222-222222222222");
        var targetA = totalCount / 2;
        var targetB = totalCount - targetA;
        var stopwatch = Stopwatch.StartNew();

        var resultA = await SeedCompanyAsync(companyAId, "A", targetA, batchSize, cancellationToken);
        var resultB = await SeedCompanyAsync(companyBId, "B", targetB, batchSize, cancellationToken);

        await _productSearchService.RefreshProductIndexAsync(cancellationToken);
        await _productSearchService.IncrementSearchCacheVersionAsync(cancellationToken);

        stopwatch.Stop();

        var sqlCountA = await CountSqlProductsAsync(companyAId, cancellationToken);
        var sqlCountB = await CountSqlProductsAsync(companyBId, cancellationToken);
        var elasticCountA = await _productSearchService.CountProductsInIndexAsync(companyAId, cancellationToken);
        var elasticCountB = await _productSearchService.CountProductsInIndexAsync(companyBId, cancellationToken);

        return new TestProductSeedResult(
            resultA.CreatedCount + resultB.CreatedCount,
            resultA.SkippedCount + resultB.SkippedCount,
            resultA.IndexedCount + resultB.IndexedCount,
            resultA.IndexErrorCount + resultB.IndexErrorCount,
            sqlCountA,
            sqlCountB,
            elasticCountA,
            elasticCountB,
            stopwatch.Elapsed);
    }

    private async Task<CompanySeedResult> SeedCompanyAsync(
        Guid companyId,
        string companyCode,
        int targetCount,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var existingCount = await _dbContext.Products
            .AsNoTracking()
            .CountAsync(product =>
                product.CompanyId == companyId &&
                product.ProductCode.StartsWith($"{ProductCodePrefix}-{companyCode}-"), cancellationToken);

        var createdCount = 0;
        var skippedCount = Math.Min(existingCount, targetCount);
        var indexedCount = 0;
        var indexErrorCount = 0;

        for (var nextNumber = existingCount + 1; nextNumber <= targetCount; nextNumber += batchSize)
        {
            var currentBatchSize = Math.Min(batchSize, targetCount - nextNumber + 1);
            var products = new List<Product>(currentBatchSize);
            var documents = new List<ProductSearchDocument>(currentBatchSize);

            for (var offset = 0; offset < currentBatchSize; offset++)
            {
                var number = nextNumber + offset;
                var code = $"{ProductCodePrefix}-{companyCode}-{number:D7}";
                var product = new Product
                {
                    Id = CreateDeterministicGuid(code),
                    CompanyId = companyId,
                    ProductCode = code,
                    ProductName = CreateProductName(companyCode, number),
                    Price = CreatePrice(number)
                };

                products.Add(product);
                documents.Add(new ProductSearchDocument
                {
                    ProductId = product.Id,
                    CompanyId = product.CompanyId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    Price = product.Price
                });
            }

            await BulkInsertProductsAsync(products, cancellationToken);

            var indexResult = await _productSearchService.BulkIndexProductsAsync(
                documents,
                refresh: false,
                cancellationToken);

            createdCount += products.Count;
            indexedCount += indexResult.IndexedCount;
            indexErrorCount += indexResult.ErrorCount;

            Console.WriteLine(
                $"{companyCode} firması: {Math.Min(nextNumber + currentBatchSize - 1, targetCount):N0}/{targetCount:N0} hazırlandı. SQL eklenen: {createdCount:N0}, ES başarılı: {indexedCount:N0}, ES hatalı: {indexErrorCount:N0}");
        }

        return new CompanySeedResult(createdCount, skippedCount, indexedCount, indexErrorCount);
    }

    private Task<int> CountSqlProductsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        return _dbContext.Products
            .AsNoTracking()
            .CountAsync(product => product.CompanyId == companyId, cancellationToken);
    }

    private async Task BulkInsertProductsAsync(IReadOnlyCollection<Product> products, CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection bulunamadı.");

        using var table = new DataTable();
        table.Columns.Add("Id", typeof(Guid));
        table.Columns.Add("FirmaId", typeof(Guid));
        table.Columns.Add("Fiyat", typeof(decimal));
        table.Columns.Add("UrunKodu", typeof(string));
        table.Columns.Add("UrunAdi", typeof(string));

        foreach (var product in products)
        {
            table.Rows.Add(
                product.Id,
                product.CompanyId,
                product.Price,
                product.ProductCode,
                product.ProductName);
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var bulkCopy = new SqlBulkCopy(connection)
        {
            DestinationTableName = "Urunler",
            BatchSize = products.Count,
            BulkCopyTimeout = 0
        };

        bulkCopy.ColumnMappings.Add("Id", "Id");
        bulkCopy.ColumnMappings.Add("FirmaId", "FirmaId");
        bulkCopy.ColumnMappings.Add("Fiyat", "Fiyat");
        bulkCopy.ColumnMappings.Add("UrunKodu", "UrunKodu");
        bulkCopy.ColumnMappings.Add("UrunAdi", "UrunAdi");

        await bulkCopy.WriteToServerAsync(table, cancellationToken);
    }

    private static Guid CreateDeterministicGuid(string value)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash);
    }

    private static string CreateProductName(string companyCode, int number)
    {
        var quality = Qualities[number % Qualities.Length];
        var productType = ProductTypes[number % ProductTypes.Length];
        var model = $"{(char)('A' + (number % 26))}{number % 1000:D3}";
        return $"Deneme {quality} {productType} {model} Firma {companyCode}";
    }

    private static decimal CreatePrice(int number)
    {
        return Math.Round(100m + ((number * 37) % 90000) + ((number % 99) / 100m), 2);
    }
}

public record TestProductSeedResult(
    int CreatedCount,
    int SkippedCount,
    int IndexedCount,
    int IndexErrorCount,
    int SqlCompanyACount,
    int SqlCompanyBCount,
    long ElasticsearchCompanyACount,
    long ElasticsearchCompanyBCount,
    TimeSpan Elapsed);

public record CompanySeedResult(
    int CreatedCount,
    int SkippedCount,
    int IndexedCount,
    int IndexErrorCount);
