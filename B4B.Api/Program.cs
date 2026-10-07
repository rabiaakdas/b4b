using B4B.Api.Data;
using B4B.Api.Services;
using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<JwtTokenService>();
builder.Services.Configure<ElasticsearchOptions>(builder.Configuration.GetSection("Elasticsearch"));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));
builder.Services.AddSingleton(sp =>
{
    var elasticsearchOptions = sp.GetRequiredService<IConfiguration>()
        .GetSection("Elasticsearch")
        .Get<ElasticsearchOptions>() ?? new ElasticsearchOptions();

    var settings = new ElasticsearchClientSettings(new Uri(elasticsearchOptions.Url))
        .DefaultIndex(elasticsearchOptions.ProductIndex);

    return new ElasticsearchClient(settings);
});
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisOptions = sp.GetRequiredService<IConfiguration>()
        .GetSection("Redis")
        .Get<RedisOptions>() ?? new RedisOptions();

    var configuration = ConfigurationOptions.Parse(redisOptions.Configuration);
    configuration.AbortOnConnectFail = false;
    configuration.ConnectTimeout = 500;
    configuration.SyncTimeout = 500;
    configuration.AsyncTimeout = 500;

    return ConnectionMultiplexer.Connect(configuration);
});
builder.Services.AddScoped<ProductSearchService>();
builder.Services.AddScoped<TestProductDataSeeder>();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key User Secrets içinde tanımlı olmalıdır.");
}

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Jwt:Key en az 32 UTF-8 byte uzunluğunda olmalıdır.");
}

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("LoginRateLimit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        context.HttpContext.Response.Headers.RetryAfter = "60";

        await context.HttpContext.Response.WriteAsync(
            JsonSerializer.Serialize(new { Message = "Çok fazla giriş denemesi yapıldı. Lütfen daha sonra tekrar deneyin." }),
            cancellationToken);
    };
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await SeedData.EnsureUsersAsync(app.Services);

if (args.Contains("--index-products", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var productSearchService = scope.ServiceProvider.GetRequiredService<ProductSearchService>();
    var result = await productSearchService.ReindexProductsAsync();

    Console.WriteLine($"Ürün indeksleme tamamlandı. Başarılı: {result.IndexedCount}, Hatalı: {result.ErrorCount}");
    return;
}

if (args.Contains("--seed-test-products", StringComparer.OrdinalIgnoreCase))
{
    var totalCount = GetIntArgument(args, "--count", 1000);
    var batchSize = GetIntArgument(args, "--batch-size", 1000);

    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<TestProductDataSeeder>();
    var result = await seeder.SeedAsync(totalCount, batchSize);

    Console.WriteLine("Deneme ürünleri hazırlandı.");
    Console.WriteLine($"SQL eklenen: {result.CreatedCount:N0}");
    Console.WriteLine($"Zaten var olan: {result.SkippedCount:N0}");
    Console.WriteLine($"Elasticsearch başarılı: {result.IndexedCount:N0}");
    Console.WriteLine($"Elasticsearch hatalı: {result.IndexErrorCount:N0}");
    Console.WriteLine($"SQL Firma A toplam: {result.SqlCompanyACount:N0}");
    Console.WriteLine($"SQL Firma B toplam: {result.SqlCompanyBCount:N0}");
    Console.WriteLine($"Elasticsearch Firma A toplam: {result.ElasticsearchCompanyACount:N0}");
    Console.WriteLine($"Elasticsearch Firma B toplam: {result.ElasticsearchCompanyBCount:N0}");
    Console.WriteLine($"Geçen süre: {result.Elapsed}");
    return;
}

app.Run();

static int GetIntArgument(string[] args, string name, int defaultValue)
{
    var index = Array.FindIndex(args, arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
    if (index < 0 || index + 1 >= args.Length)
    {
        return defaultValue;
    }

    return int.TryParse(args[index + 1], out var value) ? value : defaultValue;
}
