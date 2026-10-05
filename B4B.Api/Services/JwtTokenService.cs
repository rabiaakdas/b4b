using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using B4B.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace B4B.Api.Services;

public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(Kullanici kullanici)
    {
        var key = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Jwt:Key User Secrets içinde tanımlı değil.");
        }

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expiresMinutes = _configuration.GetValue("Jwt:ExpiresMinutes", 60);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
            new Claim(ClaimTypes.Name, kullanici.KullaniciAdi),
            new Claim("firma_id", kullanici.FirmaId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
