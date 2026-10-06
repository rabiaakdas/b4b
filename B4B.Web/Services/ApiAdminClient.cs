using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace B4B.Web.Services;

public class ApiAdminClient
{
    private readonly HttpClient _httpClient;

    public ApiAdminClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiAdminLoginResult> LoginAsync(string companyCode, string username, string password)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/admin-login",
            new AdminLoginRequestDto(companyCode, username, password));

        if (response.IsSuccessStatusCode)
        {
            var login = await response.Content.ReadFromJsonAsync<AdminLoginResponseDto>();
            return login is null
                ? ApiAdminLoginResult.Fail("API boş panel giriş yanıtı döndü.")
                : ApiAdminLoginResult.CreateSuccess(login);
        }

        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        return ApiAdminLoginResult.Fail(error?.Message ?? "Panel girişi başarısız.");
    }

    public async Task<ApiAdminUserListResult> GetUsersAsync(string token)
    {
        using var request = CreateRequest(HttpMethod.Get, "api/admin/kullanicilar", token);
        using var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var users = await response.Content.ReadFromJsonAsync<List<AdminUserDto>>();
            return users is null
                ? ApiAdminUserListResult.Fail("API boş kullanıcı listesi yanıtı döndü.")
                : ApiAdminUserListResult.CreateSuccess(users);
        }

        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        return ApiAdminUserListResult.Fail(error?.Message ?? $"Kullanıcılar alınamadı. HTTP {(int)response.StatusCode}");
    }

    public async Task<ApiAdminOrderListResult> GetOrdersAsync(string token)
    {
        using var request = CreateRequest(HttpMethod.Get, "api/admin/siparisler", token);
        using var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var orders = await response.Content.ReadFromJsonAsync<List<AdminOrderSummaryDto>>();
            return orders is null
                ? ApiAdminOrderListResult.Fail("API boş sipariş listesi yanıtı döndü.")
                : ApiAdminOrderListResult.CreateSuccess(orders);
        }

        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        return ApiAdminOrderListResult.Fail(error?.Message ?? $"Siparişler alınamadı. HTTP {(int)response.StatusCode}");
    }

    public async Task<ApiAdminOrderDetailResult> GetOrderAsync(string token, Guid orderId)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/admin/siparisler/{orderId}", token);
        using var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var order = await response.Content.ReadFromJsonAsync<AdminOrderDetailDto>();
            return order is null
                ? ApiAdminOrderDetailResult.Fail("API boş sipariş yanıtı döndü.")
                : ApiAdminOrderDetailResult.CreateSuccess(order);
        }

        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        return ApiAdminOrderDetailResult.Fail(error?.Message ?? $"Sipariş alınamadı. HTTP {(int)response.StatusCode}");
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}

public record AdminLoginRequestDto(
    [property: JsonPropertyName("firmaKodu")] string CompanyCode,
    [property: JsonPropertyName("kullaniciAdi")] string Username,
    [property: JsonPropertyName("sifre")] string Password);

public record AdminLoginResponseDto(
    string Token,
    Guid UserId,
    [property: JsonPropertyName("firmaId")] Guid CompanyId,
    [property: JsonPropertyName("kullaniciAdi")] string Username,
    [property: JsonPropertyName("firmaAdi")] string CompanyName);

public record AdminUserDto(
    Guid Id,
    [property: JsonPropertyName("kullaniciAdi")] string Username,
    [property: JsonPropertyName("adminMi")] bool IsAdmin);

public record AdminOrderSummaryDto(
    Guid Id,
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("kullaniciAdi")] string Username,
    [property: JsonPropertyName("olusturmaTarihi")] DateTime CreatedAt,
    [property: JsonPropertyName("toplamTutar")] decimal TotalAmount);

public record AdminOrderDetailDto(
    Guid Id,
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("kullaniciAdi")] string Username,
    [property: JsonPropertyName("olusturmaTarihi")] DateTime CreatedAt,
    [property: JsonPropertyName("toplamTutar")] decimal TotalAmount,
    [property: JsonPropertyName("kalemler")] List<AdminOrderItemDto> Items);

public record AdminOrderItemDto(
    Guid Id,
    [property: JsonPropertyName("urunId")] Guid ProductId,
    [property: JsonPropertyName("urunKodu")] string ProductCode,
    [property: JsonPropertyName("urunAdi")] string ProductName,
    [property: JsonPropertyName("birimFiyat")] decimal UnitPrice,
    [property: JsonPropertyName("miktar")] int Quantity,
    [property: JsonPropertyName("kalemToplami")] decimal LineTotal);

public class ApiAdminLoginResult
{
    private ApiAdminLoginResult(bool success, AdminLoginResponseDto? login, string? errorMessage)
    {
        Success = success;
        Login = login;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }

    public AdminLoginResponseDto? Login { get; }

    public string? ErrorMessage { get; }

    public static ApiAdminLoginResult CreateSuccess(AdminLoginResponseDto login)
    {
        return new ApiAdminLoginResult(true, login, null);
    }

    public static ApiAdminLoginResult Fail(string errorMessage)
    {
        return new ApiAdminLoginResult(false, null, errorMessage);
    }
}

public class ApiAdminUserListResult
{
    private ApiAdminUserListResult(bool success, List<AdminUserDto> users, string? errorMessage)
    {
        Success = success;
        Users = users;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }

    public List<AdminUserDto> Users { get; }

    public string? ErrorMessage { get; }

    public static ApiAdminUserListResult CreateSuccess(List<AdminUserDto> users)
    {
        return new ApiAdminUserListResult(true, users, null);
    }

    public static ApiAdminUserListResult Fail(string errorMessage)
    {
        return new ApiAdminUserListResult(false, [], errorMessage);
    }
}

public class ApiAdminOrderListResult
{
    private ApiAdminOrderListResult(bool success, List<AdminOrderSummaryDto> orders, string? errorMessage)
    {
        Success = success;
        Orders = orders;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }

    public List<AdminOrderSummaryDto> Orders { get; }

    public string? ErrorMessage { get; }

    public static ApiAdminOrderListResult CreateSuccess(List<AdminOrderSummaryDto> orders)
    {
        return new ApiAdminOrderListResult(true, orders, null);
    }

    public static ApiAdminOrderListResult Fail(string errorMessage)
    {
        return new ApiAdminOrderListResult(false, [], errorMessage);
    }
}

public class ApiAdminOrderDetailResult
{
    private ApiAdminOrderDetailResult(bool success, AdminOrderDetailDto? order, string? errorMessage)
    {
        Success = success;
        Order = order;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }

    public AdminOrderDetailDto? Order { get; }

    public string? ErrorMessage { get; }

    public static ApiAdminOrderDetailResult CreateSuccess(AdminOrderDetailDto order)
    {
        return new ApiAdminOrderDetailResult(true, order, null);
    }

    public static ApiAdminOrderDetailResult Fail(string errorMessage)
    {
        return new ApiAdminOrderDetailResult(false, null, errorMessage);
    }
}

