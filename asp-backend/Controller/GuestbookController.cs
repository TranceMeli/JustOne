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
    private const int MaxEntries = 100;

    private string UserId => User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

    // Public guestbook: every logged-in user sees the newest entries of everyone.
    [HttpGet]
    public async Task<IActionResult> GetEntries()
    {
        var rows = await db.GuestbookEntries
            .OrderByDescending(e => e.CreatedAt)
            .Take(MaxEntries)
            .Join(db.Users, e => e.UserId, u => u.Id,
                (e, u) => new { e.Id, e.Message, e.CreatedAt, e.UserId, u.FirstName, u.LastName, u.Email })
            .ToListAsync();

        var entries = rows.Select(r => new GuestbookEntryResponse(
            r.Id, r.Message, r.CreatedAt, UserNames.Display(r.FirstName, r.LastName, r.Email), r.UserId == UserId));

        return Ok(entries);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEntry(CreateGuestbookEntryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message must not be empty.");
        }

        var author = await db.Users.FindAsync(UserId);
        if (author is null)
        {
            return Unauthorized();
        }

        var entry = new GuestbookEntry
        {
            UserId = UserId,
            Message = request.Message.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.GuestbookEntries.Add(entry);
        await db.SaveChangesAsync();

        return Ok(new GuestbookEntryResponse(
            entry.Id, entry.Message, entry.CreatedAt, UserNames.Display(author.FirstName, author.LastName, author.Email), true));
    }
}
