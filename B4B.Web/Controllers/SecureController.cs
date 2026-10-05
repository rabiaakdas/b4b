using B4B.Web.Models;
using B4B.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace B4B.Web.Controllers;

[Authorize]
public class SecureController : Controller
{
    private const string JwtSessionKey = "ApiJwtToken";
    private readonly ApiAuthClient _apiAuthClient;
    private readonly ApiConfigClient _apiConfigClient;

    public SecureController(ApiAuthClient apiAuthClient, ApiConfigClient apiConfigClient)
    {
        _apiAuthClient = apiAuthClient;
        _apiConfigClient = apiConfigClient;
    }

    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString(JwtSessionKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        var protectedUser = await _apiAuthClient.GetProtectedUserAsync(token);
        if (protectedUser is null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        var clientHost = (HttpContext.Request.Host.Value ?? string.Empty).ToLowerInvariant();
        var configResult = await _apiConfigClient.GetConfigAsync(clientHost);
        if (!configResult.Basarili ||
            configResult.Config is null ||
            configResult.Config.Id != protectedUser.FirmaId)
        {
            HttpContext.Session.Remove(JwtSessionKey);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        return View(new ProtectedPageViewModel
        {
            UserId = protectedUser.UserId,
            FirmaId = protectedUser.FirmaId,
            KullaniciAdi = protectedUser.KullaniciAdi,
            FirmaAdi = User.FindFirst("firma_adi")?.Value ?? string.Empty
        });
    }
}
