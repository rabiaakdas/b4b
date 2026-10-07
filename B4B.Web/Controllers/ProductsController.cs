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
    public async Task<IActionResult> Index(
        [FromQuery(Name = "arama")] string? search,
        [FromQuery(Name = "sayfa")] int page = 1,
        [FromQuery(Name = "sayfaBoyutu")] int pageSize = 20)
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

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var productResult = await _apiProductClient.GetProductsAsync(token, search, page, pageSize);
        if (productResult.Unauthorized)
        {
            await SignOutAndClearSessionAsync();
            return RedirectToAction("Login", "Account");
        }

        var errorMessage = TempData["SepetHataMesaji"] as string;
        if (!productResult.Success)
        {
            errorMessage = productResult.ErrorMessage;
        }

        return View(new ProductsViewModel
        {
            Search = search,
            Page = productResult.Page == 0 ? page : productResult.Page,
            PageSize = productResult.PageSize == 0 ? pageSize : productResult.PageSize,
            TotalCount = productResult.TotalCount,
            ElasticsearchTookMilliseconds = productResult.ElasticsearchTookMilliseconds,
            SearchDurationMilliseconds = productResult.SearchDurationMilliseconds,
            CacheStatus = productResult.CacheStatus,
            ErrorMessage = errorMessage,
            Products = productResult.Products
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
    public async Task<IActionResult> AddToCart(Guid productId, int quantity, [FromForm(Name = "arama")] string? search)
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
            return RedirectToAction("Index", new { arama = search });
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

