using B4B.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    public async Task<ActionResult<FirmaConfigResponse>> GetConfig()
    {
        var clientHost = Request.Headers[ClientHostHeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(clientHost))
        {
            return BadRequest(new ErrorResponse(
                $"{ClientHostHeaderName} header bilgisi zorunludur. MVC uygulaması kendi host ve port bilgisini göndermelidir."));
        }

        var normalizedHost = clientHost.Trim().ToLowerInvariant();

        var firma = await _dbContext.Firmalar
            .AsNoTracking()
            .Where(firma => firma.Domain.ToLower() == normalizedHost)
            .Select(firma => new FirmaConfigResponse(
                firma.Id,
                firma.FirmaAdi,
                firma.ConfigDegeri))
            .FirstOrDefaultAsync();

        if (firma is null)
        {
            return NotFound(new ErrorResponse(
                $"'{normalizedHost}' adresi için kayıtlı firma bulunamadı."));
        }

        return Ok(firma);
    }
}

public record FirmaConfigResponse(int Id, string FirmaAdi, string ConfigDegeri);

public record ErrorResponse(string Mesaj);
