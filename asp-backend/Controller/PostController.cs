using JustOne.Data;
using JustOne.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using static JustOne.Models.PostModels;

namespace JustOne.Controllers;

[ApiController]
[Route("api/posts")]
[Authorize]
public class PostsController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    // Only real image formats - anything else (e.g. .html, .svg) could run script code
    // when served from our own origin via UseStaticFiles.
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private string UserId => User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

    private string UploadsFolder => Path.Combine(env.ContentRootPath, "wwwroot", "uploads");

    // GET /api/posts/calendar?year=2026&month=3
    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendar(int year, int month)
    {
        if (year is < 1 or > 9999 || month is < 1 or > 12)
        {
            return BadRequest("Invalid year or month.");
        }

        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        // Range query instead of .Year/.Month so the (UserId, CreatedAt) index can be used.
        var posts = await db.Posts
            .Where(p => p.UserId == UserId && p.CreatedAt >= monthStart && p.CreatedAt < monthEnd)
            .Select(p => new { p.Id, p.ImagePath, p.CreatedAt })
            .ToListAsync();

        // GroupBy instead of ToDictionary: a duplicate day must not crash the whole calendar.
        var postsByDay = posts
            .GroupBy(p => p.CreatedAt.Day)
            .ToDictionary(g => g.Key, g => g.First());

        var days = Enumerable.Range(1, DateTime.DaysInMonth(year, month))
            .Select(day => postsByDay.TryGetValue(day, out var post)
                ? new CalendarDayResponse(day, true, post.ImagePath, post.Id)
                : new CalendarDayResponse(day, false, null, null))
            .ToList();

        return Ok(new CalendarResponse(year, month, days));
    }

    // POST /api/posts (multipart/form-data with a file field named "file")
    [HttpPost]
    public async Task<IActionResult> Create(IFormFile file)
    {
        if (ValidateFile(file) is { } error)
        {
            return BadRequest(error);
        }

        var today = UserToday();

        var alreadyPostedToday = await db.Posts
            .AnyAsync(p => p.UserId == UserId && p.CreatedAt == today);

        if (alreadyPostedToday)
        {
            return Conflict("You have already posted today.");
        }

        var post = new Post
        {
            UserId = UserId,
            ImagePath = await SaveImageAsync(file),
            CreatedAt = today
        };

        db.Posts.Add(post);
        await db.SaveChangesAsync();

        return Ok(new PostResponse(post.Id, post.CreatedAt, post.ImagePath));
    }

    // PUT /api/posts (multipart/form-data with a new file "file")
    [HttpPut]
    public async Task<IActionResult> Update(IFormFile file)
    {
        if (ValidateFile(file) is { } error)
        {
            return BadRequest(error);
        }

        var today = UserToday();

        var post = await db.Posts
            .FirstOrDefaultAsync(p => p.UserId == UserId && p.CreatedAt == today);

        if (post is null)
        {
            return NotFound("No post found for today.");
        }

        await ReplaceImageAsync(post, file);

        return Ok(new PostResponse(post.Id, post.CreatedAt, post.ImagePath));
    }

    // DELETE /api/posts
    [HttpDelete]
    public async Task<IActionResult> Delete()
    {
        var today = UserToday();

        var post = await db.Posts
            .FirstOrDefaultAsync(p => p.UserId == UserId && p.CreatedAt == today);

        if (post is null)
        {
            return NotFound("No post found for today.");
        }

        db.Posts.Remove(post);
        await db.SaveChangesAsync();
        DeleteImageFile(post.ImagePath);

        return Ok("Post deleted.");
    }

    // PUT /api/posts/{id} (multipart/form-data with a new file "file")
    // Moderation: Admin/Manager can replace the image of ANY post, not just their own.
    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public async Task<IActionResult> ModerateUpdate(int id, IFormFile file)
    {
        if (ValidateFile(file) is { } error)
        {
            return BadRequest(error);
        }

        var post = await db.Posts.FindAsync(id);
        if (post is null)
        {
            return NotFound("Post not found.");
        }

        await ReplaceImageAsync(post, file);

        return Ok(new PostResponse(post.Id, post.CreatedAt, post.ImagePath));
    }

    // DELETE /api/posts/{id}
    // Moderation: Admin/Manager can delete ANY post, not just their own.
    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public async Task<IActionResult> ModerateDelete(int id)
    {
        var post = await db.Posts.FindAsync(id);
        if (post is null)
        {
            return NotFound("Post not found.");
        }

        db.Posts.Remove(post);
        await db.SaveChangesAsync();
        DeleteImageFile(post.ImagePath);

        return Ok("Post deleted (moderation).");
    }

    [HttpGet("{id:int}/image")]
    public async Task<IActionResult> GetImage(int id)
    {
        var imagePath = await db.Posts
            .Where(p => p.Id == id)
            .Select(p => p.ImagePath)
            .FirstOrDefaultAsync();

        return imagePath is null ? NotFound() : Ok(new { ImagePath = imagePath });
    }

    // "Today" in the user's own time zone (sent by the frontend as an IANA id, e.g. "Europe/Berlin").
    // A post's CreatedAt is that local calendar day, stored as midnight. Falls back to UTC.
    private DateTime UserToday()
    {
        var timeZone = TimeZoneInfo.Utc;
        var timeZoneId = Request.Headers["X-Timezone"].ToString();

        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            try
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                // Unknown id - keep UTC.
            }
        }

        var localDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).Date;
        return DateTime.SpecifyKind(localDate, DateTimeKind.Utc);
    }

    private static string? ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
        {
            return "No file submitted.";
        }

        if (file.Length > MaxFileSize)
        {
            return "File is too large (max. 10 MB).";
        }

        if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName)))
        {
            return "Only JPG, PNG, GIF and WebP images are allowed.";
        }

        return null;
    }

    // Saves the new image first and only then removes the old one, so a failed
    // upload never leaves the post pointing at a deleted file.
    private async Task ReplaceImageAsync(Post post, IFormFile file)
    {
        var oldImagePath = post.ImagePath;

        post.ImagePath = await SaveImageAsync(file);
        await db.SaveChangesAsync();

        DeleteImageFile(oldImagePath);
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        Directory.CreateDirectory(UploadsFolder);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        var filePath = Path.Combine(UploadsFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{fileName}";
    }

    private void DeleteImageFile(string imagePath)
    {
        var fullPath = Path.Combine(UploadsFolder, Path.GetFileName(imagePath));

        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }
}
