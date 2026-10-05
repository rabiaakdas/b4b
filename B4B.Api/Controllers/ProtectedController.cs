using System.Security.Claims;
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
        var firmaId = User.FindFirstValue("firma_id");
        var kullaniciAdi = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(firmaId))
        {
            return Unauthorized();
        }

        return Ok(new ProtectedUserResponse(
            int.Parse(userId),
            int.Parse(firmaId),
            kullaniciAdi ?? string.Empty));
    }
}

public record ProtectedUserResponse(int UserId, int FirmaId, string KullaniciAdi);
