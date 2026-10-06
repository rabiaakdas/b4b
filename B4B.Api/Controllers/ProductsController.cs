using System.Security.Claims;
using B4B.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace B4B.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/urunler")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public ProductsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductResponse>>> GetProducts([FromQuery] string? arama)
    {
        var companyIdClaim = User.FindFirstValue("firma_id");
        if (!Guid.TryParse(companyIdClaim, out var companyId))
        {
            return Unauthorized();
        }

        var query = _dbContext.Products
            .AsNoTracking()
            .Where(product => product.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(arama))
        {
            var normalizedSearch = arama.Trim();
            query = query.Where(product =>
                product.ProductName.Contains(normalizedSearch) ||
                product.ProductCode.Contains(normalizedSearch));
        }

        var products = await query
            .OrderBy(product => product.ProductCode)
            .Select(product => new ProductResponse(
                product.Id,
                product.ProductCode,
                product.ProductName,
                product.Price))
            .ToListAsync();

        return Ok(products);
    }
}

public record ProductResponse(
    Guid Id,
    [property: JsonPropertyName("urunKodu")] string ProductCode,
    [property: JsonPropertyName("urunAdi")] string ProductName,
    [property: JsonPropertyName("fiyat")] decimal Price);

