using System.Security.Claims;
using B4B.Web.Models;
using B4B.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B4B.Web.Controllers;

[Route("Admin")]
public class AdminController : Controller
{
    private const string AdminJwtSessionKey = "AdminApiJwtToken";
    private readonly ApiAdminClient _apiAdminClient;

    public AdminController(ApiAdminClient apiAdminClient)
    {
        _apiAdminClient = apiAdminClient;
    }

    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
        {
            return RedirectToAction("Index");
        }

        return View(new AdminLoginViewModel());
    }

    [HttpPost("Login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _apiAdminClient.LoginAsync(model.CompanyCode, model.Username, model.Password);
        if (!result.Success || result.Login is null)
        {
            model.ErrorMessage = result.ErrorMessage ?? "Panel girişi başarısız.";
            return View(model);
        }

        HttpContext.Session.SetString(AdminJwtSessionKey, result.Login.Token);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.Login.UserId.ToString()),
            new(ClaimTypes.Name, result.Login.Username),
            new(ClaimTypes.Role, "Admin"),
            new("firma_id", result.Login.CompanyId.ToString()),
            new("firma_adi", result.Login.CompanyName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("")]
    public IActionResult Index()
    {
        return View(new AdminPanelViewModel
        {
            CompanyName = User.FindFirst("firma_adi")?.Value ?? string.Empty,
            Username = User.Identity?.Name ?? string.Empty
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("Kullanicilar")]
    public async Task<IActionResult> Users()
    {
        var token = GetAdminToken();
        if (token is null)
        {
            return await SignOutAndRedirectAsync();
        }

        var result = await _apiAdminClient.GetUsersAsync(token);
        if (!result.Success)
        {
            return View(new AdminUsersViewModel
            {
                ErrorMessage = result.ErrorMessage ?? "Kullanıcılar alınamadı."
            });
        }

        return View(new AdminUsersViewModel
        {
            Users = result.Users
                .Select(user => new AdminUserViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    IsAdmin = user.IsAdmin
                })
                .ToList()
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("Siparisler")]
    public async Task<IActionResult> Orders()
    {
        var token = GetAdminToken();
        if (token is null)
        {
            return await SignOutAndRedirectAsync();
        }

        var result = await _apiAdminClient.GetOrdersAsync(token);
        if (!result.Success)
        {
            return View(new AdminOrdersViewModel
            {
                ErrorMessage = result.ErrorMessage ?? "Siparişler alınamadı."
            });
        }

        return View(new AdminOrdersViewModel
        {
            Orders = result.Orders
                .Select(order => new AdminOrderSummaryViewModel
                {
                    Id = order.Id,
                    Username = order.Username,
                    CreatedAt = order.CreatedAt,
                    TotalAmount = order.TotalAmount
                })
                .ToList()
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("SiparisDetay")]
    public async Task<IActionResult> OrderDetail(Guid id)
    {
        var token = GetAdminToken();
        if (token is null)
        {
            return await SignOutAndRedirectAsync();
        }

        var result = await _apiAdminClient.GetOrderAsync(token, id);
        if (!result.Success || result.Order is null)
        {
            return View(new AdminOrderDetailViewModel
            {
                ErrorMessage = result.ErrorMessage ?? "Sipariş bulunamadı."
            });
        }

        return View(new AdminOrderDetailViewModel
        {
            Id = result.Order.Id,
            Username = result.Order.Username,
            CreatedAt = result.Order.CreatedAt,
            TotalAmount = result.Order.TotalAmount,
            Items = result.Order.Items
                .Select(item => new AdminOrderItemViewModel
                {
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    LineTotal = item.LineTotal
                })
                .ToList()
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [Route("Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Remove(AdminJwtSessionKey);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Login");
    }

    private string? GetAdminToken()
    {
        return HttpContext.Session.GetString(AdminJwtSessionKey);
    }

    private async Task<IActionResult> SignOutAndRedirectAsync()
    {
        HttpContext.Session.Remove(AdminJwtSessionKey);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Login");
    }
}

