using System.Data;
using System.Security.Claims;
using System.Text.Json.Serialization;
using B4B.Api.Data;
using B4B.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace B4B.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/siparisler")]
public class OrdersController : ControllerBase
{
    private const int MinQuantity = 1;
    private const int MaxQuantity = 999;
    private readonly AppDbContext _dbContext;

    public OrdersController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<ActionResult<OrderDetailResponse>> CreateOrder()
    {
        var ownership = await GetOwnershipAsync();
        if (ownership is null)
        {
            return Unauthorized();
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var lockAcquired = await AcquireCartLockAsync(ownership.Value.CompanyId, ownership.Value.UserId);
        if (!lockAcquired)
        {
            return Conflict(new ErrorResponse("Sepetiniz için başka bir işlem devam ediyor. Lütfen tekrar deneyin."));
        }

        var cart = await _dbContext.Carts
            .Include(cart => cart.Items)
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(cart =>
                cart.CompanyId == ownership.Value.CompanyId &&
                cart.UserId == ownership.Value.UserId);

        if (cart is null || cart.Items.Count == 0)
        {
            return BadRequest(new ErrorResponse("Boş sepetten sipariş oluşturulamaz."));
        }

        if (cart.Items.Any(item =>
            item.Quantity is < MinQuantity or > MaxQuantity ||
            item.Product is null ||
            item.Product.CompanyId != ownership.Value.CompanyId))
        {
            return BadRequest(new ErrorResponse($"Sepette geçersiz ürün var veya kalem miktarı {MinQuantity} ile {MaxQuantity} arasında değil."));
        }

        var orderItems = cart.Items
            .OrderBy(item => item.Product!.ProductCode)
            .Select(item => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                ProductCode = item.Product!.ProductCode,
                ProductName = item.Product.ProductName,
                UnitPrice = item.Product.Price,
                Quantity = item.Quantity,
                LineTotal = item.Product.Price * item.Quantity
            })
            .ToList();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CompanyId = ownership.Value.CompanyId,
            UserId = ownership.Value.UserId,
            CreatedAt = DateTime.UtcNow,
            TotalAmount = orderItems.Sum(item => item.LineTotal),
            Items = orderItems
        };

        _dbContext.Orders.Add(order);
        _dbContext.CartItems.RemoveRange(cart.Items);

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        var response = ToDetailResponse(order);
        return CreatedAtAction(nameof(GetOrder), new { orderId = order.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderSummaryResponse>>> GetOrders()
    {
        var ownership = await GetOwnershipAsync();
        if (ownership is null)
        {
            return Unauthorized();
        }

        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Where(order =>
                order.CompanyId == ownership.Value.CompanyId &&
                order.UserId == ownership.Value.UserId)
            .OrderByDescending(order => order.CreatedAt)
            .Select(order => new OrderSummaryResponse(
                order.Id,
                order.CreatedAt,
                order.TotalAmount))
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<OrderDetailResponse>> GetOrder(Guid orderId)
    {
        var ownership = await GetOwnershipAsync();
        if (ownership is null)
        {
            return Unauthorized();
        }

        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order =>
                order.Id == orderId &&
                order.CompanyId == ownership.Value.CompanyId &&
                order.UserId == ownership.Value.UserId);

        if (order is null)
        {
            return NotFound(new ErrorResponse("Sipariş bulunamadı."));
        }

        return Ok(ToDetailResponse(order));
    }

    private async Task<(Guid CompanyId, Guid UserId)?> GetOwnershipAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var companyIdClaim = User.FindFirstValue("firma_id");

        if (!Guid.TryParse(userIdClaim, out var userId) ||
            !Guid.TryParse(companyIdClaim, out var companyId))
        {
            return null;
        }

        var userExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId && user.CompanyId == companyId);

        return userExists ? (companyId, userId) : null;
    }

    private async Task<bool> AcquireCartLockAsync(Guid companyId, Guid userId)
    {
        var resultParameter = new SqlParameter("@result", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        var resourceParameter = new SqlParameter("@resource", $"sepet:{companyId}:{userId}");

        await _dbContext.Database.ExecuteSqlRawAsync(
            "EXEC @result = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 0;",
            resultParameter,
            resourceParameter);

        return resultParameter.Value is int result && result >= 0;
    }

    private static OrderDetailResponse ToDetailResponse(Order order)
    {
        return new OrderDetailResponse(
            order.Id,
            order.CreatedAt,
            order.TotalAmount,
            order.Items
                .OrderBy(item => item.ProductCode)
                .Select(item => new OrderItemDetailResponse(
                    item.Id,
                    item.ProductId,
                    item.ProductCode,
                    item.ProductName,
                    item.UnitPrice,
                    item.Quantity,
                    item.LineTotal))
                .ToList());
    }
}

public record OrderSummaryResponse(
    Guid Id,
    [property: JsonPropertyName("olusturmaTarihi")] DateTime CreatedAt,
    [property: JsonPropertyName("toplamTutar")] decimal TotalAmount);

public record OrderDetailResponse(
    Guid Id,
    [property: JsonPropertyName("olusturmaTarihi")] DateTime CreatedAt,
    [property: JsonPropertyName("toplamTutar")] decimal TotalAmount,
    [property: JsonPropertyName("kalemler")] List<OrderItemDetailResponse> Items);

public record OrderItemDetailResponse(
    Guid Id,
    [property: JsonPropertyName("urunId")] Guid ProductId,
    [property: JsonPropertyName("urunKodu")] string ProductCode,
    [property: JsonPropertyName("urunAdi")] string ProductName,
    [property: JsonPropertyName("birimFiyat")] decimal UnitPrice,
    [property: JsonPropertyName("miktar")] int Quantity,
    [property: JsonPropertyName("kalemToplami")] decimal LineTotal);

