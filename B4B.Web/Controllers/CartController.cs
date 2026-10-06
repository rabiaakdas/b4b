using B4B.Web.Models;
using B4B.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B4B.Web.Controllers;

[Authorize]
[Route("Sepet")]
public class CartController : Controller
{
    private const string JwtSessionKey = "ApiJwtToken";
    private readonly ApiAuthClient _apiAuthClient;
    private readonly ApiConfigClient _apiConfigClient;
    private readonly ApiCartClient _apiCartClient;

    public CartController(
        ApiAuthClient apiAuthClient,
        ApiConfigClient apiConfigClient,
        ApiCartClient apiCartClient)
    {
        _apiAuthClient = apiAuthClient;
        _apiConfigClient = apiConfigClient;
        _apiCartClient = apiCartClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var token = await GetValidTokenOrSignOutAsync();
        if (token is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _apiCartClient.GetCartAsync(token);
        if (!result.Success || result.Cart is null)
        {
            return View(new CartViewModel
            {
                ErrorMessage = result.ErrorMessage ?? "Sepet bilgisi alınamadı."
            });
        }

        var viewModel = ToViewModel(result.Cart);
        viewModel.ErrorMessage = TempData["SepetHataMesaji"] as string;

        return View(viewModel);
    }

    [HttpPost]
    [Route("MiktarGuncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(Guid itemId, int quantity)
    {
        var token = await GetValidTokenOrSignOutAsync();
        if (token is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _apiCartClient.UpdateQuantityAsync(token, itemId, quantity);
        if (!result.Success)
        {
            TempData["SepetHataMesaji"] = result.ErrorMessage;
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [Route("KalemSil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(Guid itemId)
    {
        var token = await GetValidTokenOrSignOutAsync();
        if (token is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _apiCartClient.DeleteItemAsync(token, itemId);
        if (!result.Success)
        {
            TempData["SepetHataMesaji"] = result.ErrorMessage;
        }

        return RedirectToAction("Index");
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

    private static CartViewModel ToViewModel(CartDto cart)
    {
        return new CartViewModel
        {
            GrandTotal = cart.GrandTotal,
            Items = cart.Items
                .Select(item => new CartItemViewModel
                {
                    Id = item.Id,
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    LineTotal = item.LineTotal
                })
                .ToList()
        };
    }
}

