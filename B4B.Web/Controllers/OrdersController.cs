using B4B.Web.Models;
using B4B.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B4B.Web.Controllers;

[Authorize]
[Route("Siparisler")]
public class OrdersController : Controller
{
    private const string JwtSessionKey = "ApiJwtToken";
    private readonly ApiAuthClient _apiAuthClient;
    private readonly ApiConfigClient _apiConfigClient;
    private readonly ApiOrderClient _apiOrderClient;

    public OrdersController(
        ApiAuthClient apiAuthClient,
        ApiConfigClient apiConfigClient,
        ApiOrderClient apiOrderClient)
    {
        _apiAuthClient = apiAuthClient;
        _apiConfigClient = apiConfigClient;
        _apiOrderClient = apiOrderClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var token = await GetValidTokenOrSignOutAsync();
        if (token is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _apiOrderClient.GetOrdersAsync(token);
        if (!result.Success)
        {
            return View(new OrdersViewModel
            {
                ErrorMessage = result.ErrorMessage ?? "Siparişler alınamadı."
            });
        }

        return View(new OrdersViewModel
        {
            Orders = result.Orders
                .Select(order => new OrderSummaryViewModel
                {
                    Id = order.Id,
                    CreatedAt = order.CreatedAt,
                    TotalAmount = order.TotalAmount
                })
                .ToList(),
            ErrorMessage = TempData["SiparisHataMesaji"] as string
        });
    }

    [HttpGet("Detay")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var token = await GetValidTokenOrSignOutAsync();
        if (token is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _apiOrderClient.GetOrderAsync(token, id);
        if (!result.Success || result.Order is null)
        {
            return View(new OrderDetailViewModel
            {
                ErrorMessage = result.ErrorMessage ?? "Sipariş bulunamadı."
            });
        }

        return View(ToDetailViewModel(result.Order));
    }

    [HttpPost]
    [Route("Olustur")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create()
    {
        var token = await GetValidTokenOrSignOutAsync();
        if (token is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _apiOrderClient.CreateOrderAsync(token);
        if (!result.Success || result.Order is null)
        {
            TempData["SepetHataMesaji"] = result.ErrorMessage ?? "Sipariş oluşturulamadı.";
            return RedirectToAction("Index", "Cart");
        }

        return RedirectToAction("Detail", new { id = result.Order.Id });
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

    private static OrderDetailViewModel ToDetailViewModel(OrderDetailDto order)
    {
        return new OrderDetailViewModel
        {
            Id = order.Id,
            CreatedAt = order.CreatedAt,
            TotalAmount = order.TotalAmount,
            Items = order.Items
                .Select(item => new OrderItemDetailViewModel
                {
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

