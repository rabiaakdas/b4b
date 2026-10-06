using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace B4B.Web.Services;

public class ApiOrderClient
{
    private readonly HttpClient _httpClient;

    public ApiOrderClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiOrderListResult> GetOrdersAsync(string token)
    {
        using var request = CreateRequest(HttpMethod.Get, "api/siparisler", token);
        using var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var orders = await response.Content.ReadFromJsonAsync<List<OrderSummaryDto>>();
            return orders is null
                ? ApiOrderListResult.Fail("API boş sipariş listesi yanıtı döndü.")
                : ApiOrderListResult.CreateSuccess(orders);
        }

        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        return ApiOrderListResult.Fail(error?.Message ?? $"Siparişler alınamadı. HTTP {(int)response.StatusCode}");
    }

    public async Task<ApiOrderDetailResult> GetOrderAsync(string token, Guid orderId)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/siparisler/{orderId}", token);
        return await SendOrderRequestAsync(request);
    }

    public async Task<ApiOrderDetailResult> CreateOrderAsync(string token)
    {
        using var request = CreateRequest(HttpMethod.Post, "api/siparisler", token);
        return await SendOrderRequestAsync(request);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<ApiOrderDetailResult> SendOrderRequestAsync(HttpRequestMessage request)
    {
        using var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var order = await response.Content.ReadFromJsonAsync<OrderDetailDto>();
            return order is null
                ? ApiOrderDetailResult.Fail("API boş sipariş yanıtı döndü.")
                : ApiOrderDetailResult.CreateSuccess(order);
        }

        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        return ApiOrderDetailResult.Fail(error?.Message ?? $"Sipariş işlemi başarısız oldu. HTTP {(int)response.StatusCode}");
    }
}

public record OrderSummaryDto(
    Guid Id,
    [property: JsonPropertyName("olusturmaTarihi")] DateTime CreatedAt,
    [property: JsonPropertyName("toplamTutar")] decimal TotalAmount);

public record OrderDetailDto(
    Guid Id,
    [property: JsonPropertyName("olusturmaTarihi")] DateTime CreatedAt,
    [property: JsonPropertyName("toplamTutar")] decimal TotalAmount,
    [property: JsonPropertyName("kalemler")] List<OrderItemDetailDto> Items);

public record OrderItemDetailDto(
    Guid Id,
    [property: JsonPropertyName("urunId")] Guid ProductId,
    [property: JsonPropertyName("urunKodu")] string ProductCode,
    [property: JsonPropertyName("urunAdi")] string ProductName,
    [property: JsonPropertyName("birimFiyat")] decimal UnitPrice,
    [property: JsonPropertyName("miktar")] int Quantity,
    [property: JsonPropertyName("kalemToplami")] decimal LineTotal);

public class ApiOrderListResult
{
    private ApiOrderListResult(bool success, List<OrderSummaryDto> orders, string? errorMessage)
    {
        Success = success;
        Orders = orders;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }

    public List<OrderSummaryDto> Orders { get; }

    public string? ErrorMessage { get; }

    public static ApiOrderListResult CreateSuccess(List<OrderSummaryDto> orders)
    {
        return new ApiOrderListResult(true, orders, null);
    }

    public static ApiOrderListResult Fail(string errorMessage)
    {
        return new ApiOrderListResult(false, [], errorMessage);
    }
}

public class ApiOrderDetailResult
{
    private ApiOrderDetailResult(bool success, OrderDetailDto? order, string? errorMessage)
    {
        Success = success;
        Order = order;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }

    public OrderDetailDto? Order { get; }

    public string? ErrorMessage { get; }

    public static ApiOrderDetailResult CreateSuccess(OrderDetailDto order)
    {
        return new ApiOrderDetailResult(true, order, null);
    }

    public static ApiOrderDetailResult Fail(string errorMessage)
    {
        return new ApiOrderDetailResult(false, null, errorMessage);
    }
}

