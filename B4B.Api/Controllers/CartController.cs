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
[Route("api/sepet")]
public class CartController : ControllerBase
{
    private const int MinQuantity = 1;
    private const int MaxQuantity = 999;
    private readonly AppDbContext _dbContext;

    public CartController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<CartResponse>> GetCart()
    {
        var ownership = await GetOwnershipAsync();
        if (ownership is null)
        {
            return Unauthorized();
        }

        return Ok(await BuildCartResponseAsync(ownership.Value.CompanyId, ownership.Value.UserId));
    }

    [HttpPost("kalemler")]
    public async Task<ActionResult<CartResponse>> AddToCart(AddToCartRequest request)
    {
        var ownership = await GetOwnershipAsync();
        if (ownership is null)
        {
            return Unauthorized();
        }

        var quantityError = QuantityError(request.Quantity);
        if (quantityError is not null)
        {
            return BadRequest(new ErrorResponse(quantityError));
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var lockAcquired = await AcquireCartLockAsync(ownership.Value.CompanyId, ownership.Value.UserId);
        if (!lockAcquired)
        {
            return Conflict(new ErrorResponse("Sepetiniz için başka bir işlem devam ediyor. Lütfen tekrar deneyin."));
        }

        var product = await _dbContext.Products
            .FirstOrDefaultAsync(product => product.Id == request.ProductId && product.CompanyId == ownership.Value.CompanyId);

        if (product is null)
        {
            return NotFound(new ErrorResponse("Ürün bulunamadı veya bu firmaya ait değil."));
        }

        var cart = await GetOrCreateCartAsync(ownership.Value.CompanyId, ownership.Value.UserId);
        var item = await _dbContext.CartItems
            .FirstOrDefaultAsync(item => item.CartId == cart.Id && item.ProductId == product.Id);

        if (item is null)
        {
            _dbContext.CartItems.Add(new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = product.Id,
                Quantity = request.Quantity
            });
        }
        else
        {
            if (item.Quantity > MaxQuantity - request.Quantity)
            {
                return BadRequest(new ErrorResponse($"Kalem miktarı {MinQuantity} ile {MaxQuantity} arasında olmalıdır."));
            }

            item.Quantity += request.Quantity;
        }

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(await BuildCartResponseAsync(ownership.Value.CompanyId, ownership.Value.UserId));
    }

    [HttpPut("kalemler/{itemId:guid}")]
    public async Task<ActionResult<CartResponse>> UpdateQuantity(Guid itemId, UpdateQuantityRequest request)
    {
        var ownership = await GetOwnershipAsync();
        if (ownership is null)
        {
            return Unauthorized();
        }

        var quantityError = QuantityError(request.Quantity);
        if (quantityError is not null)
        {
            return BadRequest(new ErrorResponse(quantityError));
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var lockAcquired = await AcquireCartLockAsync(ownership.Value.CompanyId, ownership.Value.UserId);
        if (!lockAcquired)
        {
            return Conflict(new ErrorResponse("Sepetiniz için başka bir işlem devam ediyor. Lütfen tekrar deneyin."));
        }

        var item = await GetUserCartItemAsync(itemId, ownership.Value.CompanyId, ownership.Value.UserId);
        if (item is null)
        {
            return NotFound(new ErrorResponse("Sepet kalemi bulunamadı."));
        }

        item.Quantity = request.Quantity;
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(await BuildCartResponseAsync(ownership.Value.CompanyId, ownership.Value.UserId));
    }

    [HttpDelete("kalemler/{itemId:guid}")]
    public async Task<ActionResult<CartResponse>> DeleteItem(Guid itemId)
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

        var item = await GetUserCartItemAsync(itemId, ownership.Value.CompanyId, ownership.Value.UserId);
        if (item is null)
        {
            return NotFound(new ErrorResponse("Sepet kalemi bulunamadı."));
        }

        _dbContext.CartItems.Remove(item);
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(await BuildCartResponseAsync(ownership.Value.CompanyId, ownership.Value.UserId));
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

    private async Task<Cart> GetOrCreateCartAsync(Guid companyId, Guid userId)
    {
        var cart = await _dbContext.Carts
            .FirstOrDefaultAsync(cart => cart.CompanyId == companyId && cart.UserId == userId);

        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            UserId = userId
        };

        _dbContext.Carts.Add(cart);
        await _dbContext.SaveChangesAsync();

        return cart;
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

    private static string? QuantityError(int quantity)
    {
        return quantity is < MinQuantity or > MaxQuantity
            ? $"Kalem miktarı {MinQuantity} ile {MaxQuantity} arasında olmalıdır."
            : null;
    }

    private async Task<CartItem?> GetUserCartItemAsync(Guid itemId, Guid companyId, Guid userId)
    {
        return await _dbContext.CartItems
            .Include(item => item.Cart)
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item =>
                item.Id == itemId &&
                item.Cart != null &&
                item.Product != null &&
                item.Cart.CompanyId == companyId &&
                item.Cart.UserId == userId &&
                item.Product.CompanyId == companyId);
    }

    private async Task<CartResponse> BuildCartResponseAsync(Guid companyId, Guid userId)
    {
        var cart = await _dbContext.Carts
            .AsNoTracking()
            .FirstOrDefaultAsync(cart => cart.CompanyId == companyId && cart.UserId == userId);

        if (cart is null)
        {
            return new CartResponse(null, [], 0m);
        }

        var items = await _dbContext.CartItems
            .AsNoTracking()
            .Where(item =>
                item.CartId == cart.Id &&
                item.Cart != null &&
                item.Product != null &&
                item.Cart.CompanyId == companyId &&
                item.Cart.UserId == userId &&
                item.Product.CompanyId == companyId)
            .OrderBy(item => item.Product!.ProductCode)
            .Select(item => new CartItemResponse(
                item.Id,
                item.ProductId,
                item.Product!.ProductCode,
                item.Product.ProductName,
                item.Product.Price,
                item.Quantity,
                item.Product.Price * item.Quantity))
            .ToListAsync();

        return new CartResponse(
            cart.Id,
            items,
            items.Sum(item => item.LineTotal));
    }
}

public record AddToCartRequest(
    [property: JsonPropertyName("urunId")] Guid ProductId,
    [property: JsonPropertyName("miktar")] int Quantity);

public record UpdateQuantityRequest([property: JsonPropertyName("miktar")] int Quantity);

public record CartResponse(
    Guid? Id,
    [property: JsonPropertyName("kalemler")] List<CartItemResponse> Items,
    [property: JsonPropertyName("genelToplam")] decimal GrandTotal);

public record CartItemResponse(
    Guid Id,
    [property: JsonPropertyName("urunId")] Guid ProductId,
    [property: JsonPropertyName("urunKodu")] string ProductCode,
    [property: JsonPropertyName("urunAdi")] string ProductName,
    [property: JsonPropertyName("birimFiyat")] decimal UnitPrice,
    [property: JsonPropertyName("miktar")] int Quantity,
    [property: JsonPropertyName("kalemToplam")] decimal LineTotal);

