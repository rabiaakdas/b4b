using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace B4B.Web.Services;

public class ApiConfigClient
{
    private const string ClientHostHeaderName = "X-Client-Host";
    private readonly HttpClient _httpClient;

    public ApiConfigClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiConfigResult> GetConfigAsync(string clientHost)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/config");
        request.Headers.Add(ClientHostHeaderName, clientHost);

        using var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var config = await response.Content.ReadFromJsonAsync<CompanyConfigDto>();

            return config is null
                ? ApiConfigResult.Fail("API boş yapılandırma yanıtı döndü.")
                : ApiConfigResult.CreateSuccess(config);
        }

        var apiError = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        var message = apiError?.Message ?? $"API yapılandırma bilgisi dönmedi. HTTP {(int)response.StatusCode}";

        return ApiConfigResult.Fail(message);
    }
}

public record CompanyConfigDto(
    Guid Id,
    [property: JsonPropertyName("firmaAdi")] string CompanyName,
    [property: JsonPropertyName("configDegeri")] string ConfigValue);

public record ApiErrorDto([property: JsonPropertyName("mesaj")] string Message);

public class ApiConfigResult
{
    private ApiConfigResult(bool success, CompanyConfigDto? config, string? errorMessage)
    {
        Success = success;
        Config = config;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }

    public CompanyConfigDto? Config { get; }

    public string? ErrorMessage { get; }

    public static ApiConfigResult CreateSuccess(CompanyConfigDto config)
    {
        return new ApiConfigResult(true, config, null);
    }

    public static ApiConfigResult Fail(string errorMessage)
    {
        return new ApiConfigResult(false, null, errorMessage);
    }
}

