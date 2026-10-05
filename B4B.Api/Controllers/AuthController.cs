using B4B.Api.Data;
using B4B.Api.Models;
using B4B.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace B4B.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string ClientHostHeaderName = "X-Client-Host";
    private readonly AppDbContext _dbContext;
    private readonly JwtTokenService _jwtTokenService;
    private readonly PasswordHasher<Kullanici> _passwordHasher = new();

    public AuthController(AppDbContext dbContext, JwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var clientHost = Request.Headers[ClientHostHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(clientHost))
        {
            return BadRequest(new ErrorResponse($"{ClientHostHeaderName} header bilgisi zorunludur."));
        }

        var normalizedHost = clientHost.Trim().ToLowerInvariant();
        var firma = await _dbContext.Firmalar
            .AsNoTracking()
            .FirstOrDefaultAsync(firma => firma.Domain.ToLower() == normalizedHost);

        if (firma is null)
        {
            return Unauthorized(new ErrorResponse("Firma bulunamadı."));
        }

        var kullanici = await _dbContext.Kullanicilar
            .FirstOrDefaultAsync(kullanici =>
                kullanici.FirmaId == firma.Id &&
                kullanici.KullaniciAdi == request.KullaniciAdi);

        if (kullanici is null)
        {
            return Unauthorized(new ErrorResponse("Kullanıcı adı veya şifre hatalı."));
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            kullanici,
            kullanici.SifreHash,
            request.Sifre);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new ErrorResponse("Kullanıcı adı veya şifre hatalı."));
        }

        var token = _jwtTokenService.CreateToken(kullanici);

        return Ok(new LoginResponse(
            token,
            kullanici.Id,
            firma.Id,
            kullanici.KullaniciAdi,
            firma.FirmaAdi));
    }
}

public record LoginRequest(string KullaniciAdi, string Sifre);

public record LoginResponse(
    string Token,
    int UserId,
    int FirmaId,
    string KullaniciAdi,
    string FirmaAdi);
