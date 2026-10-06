using B4B.Api.Data;
using B4B.Api.Models;
using B4B.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace B4B.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string ClientHostHeaderName = "X-Client-Host";
    private readonly AppDbContext _dbContext;
    private readonly JwtTokenService _jwtTokenService;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthController(AppDbContext dbContext, JwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("LoginRateLimit")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var clientHost = Request.Headers[ClientHostHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(clientHost))
        {
            return BadRequest(new ErrorResponse($"{ClientHostHeaderName} header bilgisi zorunludur."));
        }

        var normalizedHost = clientHost.Trim().ToLowerInvariant();
        var company = await _dbContext.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(company => company.Domain.ToLower() == normalizedHost);

        if (company is null)
        {
            return Unauthorized(new ErrorResponse("Firma bulunamadı."));
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(user =>
                user.CompanyId == company.Id &&
                user.Username == request.Username);

        if (user is null)
        {
            return Unauthorized(new ErrorResponse("Kullanıcı adı veya şifre hatalı."));
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new ErrorResponse("Kullanıcı adı veya şifre hatalı."));
        }

        var token = _jwtTokenService.CreateToken(user);

        return Ok(new LoginResponse(
            token,
            user.Id,
            company.Id,
            user.Username,
            company.CompanyName));
    }

    [HttpPost("admin-login")]
    [EnableRateLimiting("LoginRateLimit")]
    public async Task<ActionResult<AdminLoginResponse>> AdminLogin(AdminLoginRequest request)
    {
        const string generalErrorMessage = "Firma kodu, kullanıcı adı veya şifre hatalı.";

        var normalizedCompanyCode = NormalizeCompanyCode(request.CompanyCode);
        if (string.IsNullOrWhiteSpace(normalizedCompanyCode))
        {
            return Unauthorized(new ErrorResponse(generalErrorMessage));
        }

        var company = await _dbContext.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(company => company.CompanyCode == normalizedCompanyCode);

        if (company is null)
        {
            return Unauthorized(new ErrorResponse(generalErrorMessage));
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(user =>
                user.CompanyId == company.Id &&
                user.Username == request.Username);

        if (user is null)
        {
            return Unauthorized(new ErrorResponse(generalErrorMessage));
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed || !user.IsAdmin)
        {
            return Unauthorized(new ErrorResponse(generalErrorMessage));
        }

        var token = _jwtTokenService.CreateToken(user);

        return Ok(new AdminLoginResponse(
            token,
            user.Id,
            company.Id,
            user.Username,
            company.CompanyName));
    }

    private static string NormalizeCompanyCode(string? companyCode)
    {
        return companyCode?.Trim().ToUpperInvariant() ?? string.Empty;
    }
}

public record LoginRequest(
    [property: JsonPropertyName("kullaniciAdi")] string Username,
    [property: JsonPropertyName("sifre")] string Password);

public record AdminLoginRequest(
    [property: JsonPropertyName("firmaKodu")] string CompanyCode,
    [property: JsonPropertyName("kullaniciAdi")] string Username,
    [property: JsonPropertyName("sifre")] string Password);

public record LoginResponse(
    string Token,
    Guid UserId,
    [property: JsonPropertyName("firmaId")] Guid CompanyId,
    [property: JsonPropertyName("kullaniciAdi")] string Username,
    [property: JsonPropertyName("firmaAdi")] string CompanyName);

public record AdminLoginResponse(
    string Token,
    Guid UserId,
    [property: JsonPropertyName("firmaId")] Guid CompanyId,
    [property: JsonPropertyName("kullaniciAdi")] string Username,
    [property: JsonPropertyName("firmaAdi")] string CompanyName);

