using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace B4B.Web.Services;

public class ApiAuthClient
{
    private const string ClientHostHeaderName = "X-Client-Host";
    private readonly HttpClient _httpClient;

    public ApiAuthClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiLoginResult> LoginAsync(string clientHost, string username, string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login");
        request.Headers.Add(ClientHostHeaderName, clientHost);
        request.Content = JsonContent.Create(new LoginRequestDto(username, password));

        using var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            return login is null
                ? ApiLoginResult.Fail("API boş giriş yanıtı döndü.")
                : ApiLoginResult.CreateSuccess(login);
        }

        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        return ApiLoginResult.Fail(error?.Message ?? "Giriş başarısız.");
    }

    public async Task<ProtectedUserDto?> GetProtectedUserAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/protected/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ProtectedUserDto>();
    }
}

public record LoginRequestDto(
    [property: JsonPropertyName("kullaniciAdi")] string Username,
    [property: JsonPropertyName("sifre")] string Password);

public record LoginResponseDto(
    string Token,
    Guid UserId,
    [property: JsonPropertyName("firmaId")] Guid CompanyId,
    [property: JsonPropertyName("kullaniciAdi")] string Username,
    [property: JsonPropertyName("firmaAdi")] string CompanyName);

public record ProtectedUserDto(
    Guid UserId,
    [property: JsonPropertyName("firmaId")] Guid CompanyId,
    [property: JsonPropertyName("kullaniciAdi")] string Username);

public class ApiLoginResult
{
    private ApiLoginResult(bool success, LoginResponseDto? login, string? errorMessage)
    {
        Success = success;
        Login = login;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }

    public LoginResponseDto? Login { get; }

    public string? ErrorMessage { get; }

    public static ApiLoginResult CreateSuccess(LoginResponseDto login)
    {
        return new ApiLoginResult(true, login, null);
    }

    public static ApiLoginResult Fail(string errorMessage)
    {
        return new ApiLoginResult(false, null, errorMessage);
    }
}

