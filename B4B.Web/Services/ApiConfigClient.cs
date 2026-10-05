using System.Net.Http.Json;

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
            var config = await response.Content.ReadFromJsonAsync<FirmaConfigDto>();

            return config is null
                ? ApiConfigResult.Fail("API boş yapılandırma yanıtı döndü.")
                : ApiConfigResult.Success(config);
        }

        var apiError = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        var message = apiError?.Mesaj ?? $"API yapılandırma bilgisi dönmedi. HTTP {(int)response.StatusCode}";

        return ApiConfigResult.Fail(message);
    }
}

public record FirmaConfigDto(int Id, string FirmaAdi, string ConfigDegeri);

public record ApiErrorDto(string Mesaj);

public class ApiConfigResult
{
    private ApiConfigResult(bool basarili, FirmaConfigDto? config, string? hataMesaji)
    {
        Basarili = basarili;
        Config = config;
        HataMesaji = hataMesaji;
    }

    public bool Basarili { get; }

    public FirmaConfigDto? Config { get; }

    public string? HataMesaji { get; }

    public static ApiConfigResult Success(FirmaConfigDto config)
    {
        return new ApiConfigResult(true, config, null);
    }

    public static ApiConfigResult Fail(string hataMesaji)
    {
        return new ApiConfigResult(false, null, hataMesaji);
    }
}
