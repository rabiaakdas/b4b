using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace B4B.Web.Services;

public class ApiAuthClient
{
    private const string ClientHostHeaderName = "X-Client-Host";
    private readonly HttpClient _httpClient;

    public ApiAuthClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiLoginResult> LoginAsync(string clientHost, string kullaniciAdi, string sifre)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login");
        request.Headers.Add(ClientHostHeaderName, clientHost);
        request.Content = JsonContent.Create(new LoginRequestDto(kullaniciAdi, sifre));

        using var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            return login is null
                ? ApiLoginResult.Fail("API boş giriş yanıtı döndü.")
                : ApiLoginResult.Success(login);
        }

        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        return ApiLoginResult.Fail(error?.Mesaj ?? "Giriş başarısız.");
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

public record LoginRequestDto(string KullaniciAdi, string Sifre);

public record LoginResponseDto(
    string Token,
    int UserId,
    int FirmaId,
    string KullaniciAdi,
    string FirmaAdi);

public record ProtectedUserDto(int UserId, int FirmaId, string KullaniciAdi);

public class ApiLoginResult
{
    private ApiLoginResult(bool basarili, LoginResponseDto? login, string? hataMesaji)
    {
        Basarili = basarili;
        Login = login;
        HataMesaji = hataMesaji;
    }

    public bool Basarili { get; }

    public LoginResponseDto? Login { get; }

    public string? HataMesaji { get; }

    public static ApiLoginResult Success(LoginResponseDto login)
    {
        return new ApiLoginResult(true, login, null);
    }

    public static ApiLoginResult Fail(string hataMesaji)
    {
        return new ApiLoginResult(false, null, hataMesaji);
    }
}
