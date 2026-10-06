using B4B.Web.Models;
using B4B.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B4B.Web.Controllers;

[Authorize]
[Route("Urunler")]
public class ProductsController : Controller
{
    private const string JwtSessionKey = "ApiJwtToken";
    private readonly ApiAuthClient _apiAuthClient;
    private readonly ApiConfigClient _apiConfigClient;
    private readonly ApiProductClient _apiProductClient;
    private readonly ApiCartClient _apiCartClient;

    public ProductsController(
        ApiAuthClient apiAuthClient,
        ApiConfigClient apiConfigClient,
        ApiProductClient apiProductClient,
        ApiCartClient apiCartClient)
    {
        _apiAuthClient = apiAuthClient;
        _apiConfigClient = apiConfigClient;
        _apiProductClient = apiProductClient;
        _apiCartClient = apiCartClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? arama)
    {
        var token = HttpContext.Session.GetString(JwtSessionKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            await SignOutAndClearSessionAsync();
            return RedirectToAction("Login", "Account");
        }

        var protectedUser = await _apiAuthClient.GetProtectedUserAsync(token);
        if (protectedUser is null)
        {
            await SignOutAndClearSessionAsync();
            return RedirectToAction("Login", "Account");
        }

        var clientHost = (HttpContext.Request.Host.Value ?? string.Empty).ToLowerInvariant();
        var configResult = await _apiConfigClient.GetConfigAsync(clientHost);
        if (!configResult.Success ||
            configResult.Config is null ||
            configResult.Config.Id != protectedUser.CompanyId)
        {
            await SignOutAndClearSessionAsync();
            return RedirectToAction("Login", "Account");
        }

        var products = await _apiProductClient.GetProductsAsync(token, arama);
        if (products is null)
        {
            await SignOutAndClearSessionAsync();
            return RedirectToAction("Login", "Account");
        }

        return View(new ProductsViewModel
        {
            Arama = arama,
            ErrorMessage = TempData["SepetHataMesaji"] as string,
            Products = products
                .Select(product => new ProductViewModel
                {
                    Id = product.Id,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    Price = product.Price
                })
                .ToList()
        });
    }

    [HttpPost]
    [Route("SepeteEkle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(Guid productId, int quantity, string? arama)
    {
        var token = await GetValidTokenOrSignOutAsync();
        if (token is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _apiCartClient.AddToCartAsync(token, productId, quantity);
        if (!result.Success)
        {
            TempData["SepetHataMesaji"] = result.ErrorMessage;
            return RedirectToAction("Index", new { arama });
        }

        return RedirectToAction("Index", "Cart");
    }

    private async Task<string?> GetValidTokenOrSignOutAsync()
    {
        var token = HttpContext.Session.GetString(JwtSessionKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            await SignOutAndClearSessionAsync();
            return null;
        }

        var protectedUser = await _apiAuthClient.GetProtectedUserAsync(token);
        if (protectedUser is null)
        {
            await SignOutAndClearSessionAsync();
            return null;
        }

        var clientHost = (HttpContext.Request.Host.Value ?? string.Empty).ToLowerInvariant();
        var configResult = await _apiConfigClient.GetConfigAsync(clientHost);
        if (!configResult.Success ||
            configResult.Config is null ||
            configResult.Config.Id != protectedUser.CompanyId)
        {
            await SignOutAndClearSessionAsync();
            return null;
        }

        return token;
    }

    private async Task SignOutAndClearSessionAsync()
    {
        HttpContext.Session.Remove(JwtSessionKey);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}

