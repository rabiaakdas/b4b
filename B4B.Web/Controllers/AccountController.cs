using System.Security.Claims;
using B4B.Web.Models;
using B4B.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B4B.Web.Controllers;

public class AccountController : Controller
{
    private const string JwtSessionKey = "ApiJwtToken";
    private readonly ApiAuthClient _apiAuthClient;

    public AccountController(ApiAuthClient apiAuthClient)
    {
        _apiAuthClient = apiAuthClient;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel
        {
            ClientHost = GetClientHost()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        model.ClientHost = GetClientHost();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _apiAuthClient.LoginAsync(model.ClientHost, model.Username, model.Password);
        if (!result.Success || result.Login is null)
        {
            model.ErrorMessage = result.ErrorMessage ?? "Giriş başarısız.";
            return View(model);
        }

        HttpContext.Session.SetString(JwtSessionKey, result.Login.Token);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.Login.UserId.ToString()),
            new(ClaimTypes.Name, result.Login.Username),
            new("firma_id", result.Login.CompanyId.ToString()),
            new("firma_adi", result.Login.CompanyName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToAction("Index", "Secure");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Remove(JwtSessionKey);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Home");
    }

    private string GetClientHost()
    {
        return (HttpContext.Request.Host.Value ?? string.Empty).ToLowerInvariant();
    }
}
