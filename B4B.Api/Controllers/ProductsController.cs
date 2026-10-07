using System.Security.Claims;
using B4B.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace B4B.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/urunler")]
public class ProductsController : ControllerBase
{
    private readonly ProductSearchService _productSearchService;

    public ProductsController(ProductSearchService productSearchService)
    {
        _productSearchService = productSearchService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductResponse>>> GetProducts(
        [FromQuery(Name = "arama")] string? search,
        [FromQuery(Name = "sayfa")] int page = 1,
        [FromQuery(Name = "sayfaBoyutu")] int pageSize = 100)
    {
        var companyIdClaim = User.FindFirstValue("firma_id");
        if (!Guid.TryParse(companyIdClaim, out var companyId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _productSearchService.SearchProductsAsync(
                companyId,
                search,
                page,
                pageSize,
                HttpContext.RequestAborted);

            Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
            Response.Headers["X-Search-Took-Ms"] = result.ElasticsearchElapsedMilliseconds.ToString();
            Response.Headers["X-Search-Duration-Ms"] = result.DurationMilliseconds.ToString();
            Response.Headers["X-Page"] = result.Page.ToString();
            Response.Headers["X-Page-Size"] = result.PageSize.ToString();
            Response.Headers["X-Cache"] = result.CacheStatus;

            var products = result.Products
                .Select(product => new ProductResponse(
                    product.ProductId,
                    product.ProductCode,
                    product.ProductName,
                    product.Price))
                .ToList();

            return Ok(products);
        }
        catch (ProductSearchUnavailableException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { Message = exception.Message });
        }
        catch (ProductSearchPageTooDeepException exception)
        {
            return BadRequest(new { Message = exception.Message });
        }
    }
}

public record ProductResponse(
    Guid Id,
    [property: JsonPropertyName("urunKodu")] string ProductCode,
    [property: JsonPropertyName("urunAdi")] string ProductName,
    [property: JsonPropertyName("fiyat")] decimal Price);

