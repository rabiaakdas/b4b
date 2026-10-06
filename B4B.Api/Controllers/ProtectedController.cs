using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B4B.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProtectedController : ControllerBase
{
    [HttpGet("me")]
    public ActionResult<ProtectedUserResponse> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var companyId = User.FindFirstValue("firma_id");
        var username = User.Identity?.Name;

        if (!Guid.TryParse(userId, out var parsedUserId) ||
            !Guid.TryParse(companyId, out var parsedCompanyId))
        {
            return Unauthorized();
        }

        return Ok(new ProtectedUserResponse(
            parsedUserId,
            parsedCompanyId,
            username ?? string.Empty));
    }
}

public record ProtectedUserResponse(
    Guid UserId,
    [property: JsonPropertyName("firmaId")] Guid CompanyId,
    [property: JsonPropertyName("kullaniciAdi")] string Username);

