using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace B4B.Web.Services;

public class ApiCartClient
{
    private readonly HttpClient _httpClient;

    public ApiCartClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiCartResult> GetCartAsync(string token)
    {
        using var request = CreateRequest(HttpMethod.Get, "api/sepet", token);
        return await SendCartRequestAsync(request);
    }

    public async Task<ApiCartResult> AddToCartAsync(string token, Guid productId, int quantity)
    {
        using var request = CreateRequest(HttpMethod.Post, "api/sepet/kalemler", token);
        request.Content = JsonContent.Create(new AddToCartDto(productId, quantity));
        return await SendCartRequestAsync(request);
    }

    public async Task<ApiCartResult> UpdateQuantityAsync(string token, Guid itemId, int quantity)
    {
        using var request = CreateRequest(HttpMethod.Put, $"api/sepet/kalemler/{itemId}", token);
        request.Content = JsonContent.Create(new UpdateQuantityDto(quantity));
        return await SendCartRequestAsync(request);
    }

    public async Task<ApiCartResult> DeleteItemAsync(string token, Guid itemId)
    {
        using var request = CreateRequest(HttpMethod.Delete, $"api/sepet/kalemler/{itemId}", token);
        return await SendCartRequestAsync(request);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<ApiCartResult> SendCartRequestAsync(HttpRequestMessage request)
    {
        using var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var cart = await response.Content.ReadFromJsonAsync<CartDto>();
            return cart is null
                ? ApiCartResult.Fail("API boş sepet yanıtı döndü.")
                : ApiCartResult.CreateSuccess(cart);
        }

        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        return ApiCartResult.Fail(error?.Message ?? $"Sepet işlemi başarısız oldu. HTTP {(int)response.StatusCode}");
    }
}

public record AddToCartDto(
    [property: JsonPropertyName("urunId")] Guid ProductId,
    [property: JsonPropertyName("miktar")] int Quantity);

public record UpdateQuantityDto([property: JsonPropertyName("miktar")] int Quantity);

public record CartDto(
    Guid? Id,
    [property: JsonPropertyName("kalemler")] List<CartItemDto> Items,
    [property: JsonPropertyName("genelToplam")] decimal GrandTotal);

public record CartItemDto(
    Guid Id,
    [property: JsonPropertyName("urunId")] Guid ProductId,
    [property: JsonPropertyName("urunKodu")] string ProductCode,
    [property: JsonPropertyName("urunAdi")] string ProductName,
    [property: JsonPropertyName("birimFiyat")] decimal UnitPrice,
    [property: JsonPropertyName("miktar")] int Quantity,
    [property: JsonPropertyName("kalemToplam")] decimal LineTotal);

public class ApiCartResult
{
    private ApiCartResult(bool success, CartDto? cart, string? errorMessage)
    {
        Success = success;
        Cart = cart;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }

    public CartDto? Cart { get; }

    public string? ErrorMessage { get; }

    public static ApiCartResult CreateSuccess(CartDto cart)
    {
        return new ApiCartResult(true, cart, null);
    }

    public static ApiCartResult Fail(string errorMessage)
    {
        return new ApiCartResult(false, null, errorMessage);
    }
}

