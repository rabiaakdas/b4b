using System.Security.Claims;
using System.Text.Json.Serialization;
using B4B.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace B4B.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AdminController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("kullanicilar")]
    public async Task<ActionResult<List<AdminUserResponse>>> GetUsers()
    {
        var admin = await GetAdminAsync();
        if (admin is null)
        {
            return Unauthorized();
        }

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.CompanyId == admin.Value.CompanyId)
            .OrderBy(user => user.Username)
            .Select(user => new AdminUserResponse(
                user.Id,
                user.Username,
                user.IsAdmin))
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("siparisler")]
    public async Task<ActionResult<List<AdminOrderSummaryResponse>>> GetOrders()
    {
        var admin = await GetAdminAsync();
        if (admin is null)
        {
            return Unauthorized();
        }

        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.CompanyId == admin.Value.CompanyId)
            .OrderByDescending(order => order.CreatedAt)
            .Select(order => new AdminOrderSummaryResponse(
                order.Id,
                order.UserId,
                order.User != null ? order.User.Username : string.Empty,
                order.CreatedAt,
                order.TotalAmount))
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("siparisler/{orderId:guid}")]
    public async Task<ActionResult<AdminOrderDetailResponse>> GetOrder(Guid orderId)
    {
        var admin = await GetAdminAsync();
        if (admin is null)
        {
            return Unauthorized();
        }

        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.User)
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order =>
                order.Id == orderId &&
                order.CompanyId == admin.Value.CompanyId);

        if (order is null)
        {
            return NotFound(new ErrorResponse("Sipariş bulunamadı."));
        }

        return Ok(new AdminOrderDetailResponse(
            order.Id,
            order.UserId,
            order.User?.Username ?? string.Empty,
            order.CreatedAt,
            order.TotalAmount,
            order.Items
                .OrderBy(item => item.ProductCode)
                .Select(item => new AdminOrderItemResponse(
                    item.Id,
                    item.ProductId,
                    item.ProductCode,
                    item.ProductName,
                    item.UnitPrice,
                    item.Quantity,
                    item.LineTotal))
                .ToList()));
    }

    private async Task<(Guid CompanyId, Guid UserId)?> GetAdminAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var companyIdClaim = User.FindFirstValue("firma_id");
        var isAdminClaim = User.FindFirstValue("is_admin");

        if (!Guid.TryParse(userIdClaim, out var userId) ||
            !Guid.TryParse(companyIdClaim, out var companyId) ||
            !string.Equals(isAdminClaim, "true", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var adminVar = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user =>
                user.Id == userId &&
                user.CompanyId == companyId &&
                user.IsAdmin);

        return adminVar ? (companyId, userId) : null;
    }
}

public record AdminUserResponse(
    Guid Id,
    [property: JsonPropertyName("kullaniciAdi")] string Username,
    [property: JsonPropertyName("adminMi")] bool IsAdmin);

public record AdminOrderSummaryResponse(
    Guid Id,
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("kullaniciAdi")] string Username,
    [property: JsonPropertyName("olusturmaTarihi")] DateTime CreatedAt,
    [property: JsonPropertyName("toplamTutar")] decimal TotalAmount);

public record AdminOrderDetailResponse(
    Guid Id,
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("kullaniciAdi")] string Username,
    [property: JsonPropertyName("olusturmaTarihi")] DateTime CreatedAt,
    [property: JsonPropertyName("toplamTutar")] decimal TotalAmount,
    [property: JsonPropertyName("kalemler")] List<AdminOrderItemResponse> Items);

public record AdminOrderItemResponse(
    Guid Id,
    [property: JsonPropertyName("urunId")] Guid ProductId,
    [property: JsonPropertyName("urunKodu")] string ProductCode,
    [property: JsonPropertyName("urunAdi")] string ProductName,
    [property: JsonPropertyName("birimFiyat")] decimal UnitPrice,
    [property: JsonPropertyName("miktar")] int Quantity,
    [property: JsonPropertyName("kalemToplami")] decimal LineTotal);

