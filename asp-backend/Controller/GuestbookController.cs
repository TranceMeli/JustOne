using JustOne.Data;
using JustOne.Entities;
using JustOne.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace JustOne.Controllers;

[ApiController]
[Route("api/guestbook")]
[Authorize]
public class GuestbookController(AppDbContext db) : ControllerBase
{
    private string UserId => User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

    // For now: everyone only sees their own entries.
    // A shared, public guestbook is planned as a later addition.
    [HttpGet]
    public async Task<IActionResult> GetOwnEntries()
    {
        var entries = await db.GuestbookEntries
            .Where(e => e.UserId == UserId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new GuestbookEntryResponse(e.Id, e.Message, e.CreatedAt))
            .ToListAsync();

        return Ok(entries);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEntry(CreateGuestbookEntryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message must not be empty.");
        }

        var entry = new GuestbookEntry
        {
            UserId = UserId,
            Message = request.Message.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.GuestbookEntries.Add(entry);
        await db.SaveChangesAsync();

        return Ok(new GuestbookEntryResponse(entry.Id, entry.Message, entry.CreatedAt));
    }
}