using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class GoogleCalendarController : ControllerBase
{
    private static readonly Dictionary<int, string> UserTokens = new();

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        return Ok(new { IsConnected = UserTokens.ContainsKey(userId) });
    }

    [HttpPost("connect")]
    public IActionResult Connect()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        // Simulate OAuth success
        UserTokens[userId] = "mock-google-token-" + Guid.NewGuid();
        return Ok(new { Success = true, Message = "Connesso con successo a Google Calendar!" });
    }

    [HttpPost("sync")]
    public IActionResult Sync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        if (!UserTokens.ContainsKey(userId)) return BadRequest("Non connesso a Google");

        // Simulate sync logic
        return Ok(new {
            Success = true,
            SyncedCount = 5,
            Message = "Sincronizzazione completata: 5 eventi aggiornati."
        });
    }

    [HttpPost("disconnect")]
    public IActionResult Disconnect()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        UserTokens.Remove(userId);
        return Ok(new { Success = true });
    }
}
