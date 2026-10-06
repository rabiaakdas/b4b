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

    public async Task<List<ProductDto>?> GetProductsAsync(string token, string? arama)
    {
        var url = string.IsNullOrWhiteSpace(arama)
            ? "api/urunler"
            : $"api/urunler?arama={Uri.EscapeDataString(arama.Trim())}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<List<ProductDto>>();
    }
}

public record ProductDto(
    Guid Id,
    [property: JsonPropertyName("urunKodu")] string ProductCode,
    [property: JsonPropertyName("urunAdi")] string ProductName,
    [property: JsonPropertyName("fiyat")] decimal Price);

