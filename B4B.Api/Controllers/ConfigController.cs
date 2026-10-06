using B4B.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace B4B.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private const string ClientHostHeaderName = "X-Client-Host";
    private readonly AppDbContext _dbContext;

    public ConfigController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<CompanyConfigResponse>> GetConfig()
    {
        var clientHost = Request.Headers[ClientHostHeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(clientHost))
        {
            return BadRequest(new ErrorResponse(
                $"{ClientHostHeaderName} header bilgisi zorunludur. MVC uygulaması kendi host ve port bilgisini göndermelidir."));
        }

        var normalizedHost = clientHost.Trim().ToLowerInvariant();

        var companyConfig = await _dbContext.Companies
            .AsNoTracking()
            .Where(company => company.Domain.ToLower() == normalizedHost)
            .Select(company => new CompanyConfigResponse(
                company.Id,
                company.CompanyName,
                company.ConfigValue))
            .FirstOrDefaultAsync();

        if (companyConfig is null)
        {
            return NotFound(new ErrorResponse(
                $"'{normalizedHost}' adresi için kayıtlı firma bulunamadı."));
        }

        return Ok(companyConfig);
    }
}

public record CompanyConfigResponse(
    Guid Id,
    [property: JsonPropertyName("firmaAdi")] string CompanyName,
    [property: JsonPropertyName("configDegeri")] string ConfigValue);

public record ErrorResponse([property: JsonPropertyName("mesaj")] string Message);

