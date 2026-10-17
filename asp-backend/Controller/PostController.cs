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
    private string UserId => User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

    // GET /api/posts/calendar?year=2026&month=3
    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendar(int year, int month)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);

        var postsByDay = await db.Posts
            .Where(p => p.UserId == UserId && p.CreatedAt.Year == year && p.CreatedAt.Month == month)
            .ToDictionaryAsync(p => p.CreatedAt.Day);

        var days = Enumerable.Range(1, daysInMonth)
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
        if (file.Length == 0)
        {
            return BadRequest("No file submitted.");
        }

        var today = DateTime.UtcNow.Date;

        var alreadyPostedToday = await db.Posts
            .AnyAsync(p => p.UserId == UserId && p.CreatedAt.Date == today);

        if (alreadyPostedToday)
        {
            return Conflict("You have already posted today.");
        }

        var uploadsFolder = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var post = new Post
        {
            UserId = UserId,
            ImagePath = $"/uploads/{fileName}",
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
        if (file.Length == 0)
        {
            return BadRequest("No file submitted.");
        }

        var today = DateTime.UtcNow.Date;

        var post = await db.Posts
            .FirstOrDefaultAsync(p => p.UserId == UserId && p.CreatedAt.Date == today);

        if (post is null)
        {
            return NotFound("No post found for today.");
        }

        DeleteImageFile(post.ImagePath);

        var uploadsFolder = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        post.ImagePath = $"/uploads/{fileName}";
        await db.SaveChangesAsync();

        return Ok(new PostResponse(post.Id, post.CreatedAt, post.ImagePath));
    }

    // DELETE /api/posts
    [HttpDelete]
    public async Task<IActionResult> Delete()
    {
        var today = DateTime.UtcNow.Date;

        var post = await db.Posts
            .FirstOrDefaultAsync(p => p.UserId == UserId && p.CreatedAt.Date == today);

        if (post is null)
        {
            return NotFound("No post found for today.");
        }

        DeleteImageFile(post.ImagePath);

        db.Posts.Remove(post);
        await db.SaveChangesAsync();

        return Ok("Post deleted.");
    }

    // PUT /api/posts/{id} (multipart/form-data with a new file "file")
    // Moderation: Admin/Manager can replace the image of ANY post, not just their own.
    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public async Task<IActionResult> ModerateUpdate(int id, IFormFile file)
    {
        if (file.Length == 0)
        {
            return BadRequest("No file submitted.");
        }

        var post = await db.Posts.FindAsync(id);
        if (post is null)
        {
            return NotFound("Post not found.");
        }

        DeleteImageFile(post.ImagePath);

        var uploadsFolder = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        post.ImagePath = $"/uploads/{fileName}";
        await db.SaveChangesAsync();

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

        DeleteImageFile(post.ImagePath);

        db.Posts.Remove(post);
        await db.SaveChangesAsync();

        return Ok("Post deleted (moderation).");
    }

    [HttpGet("{id:int}/image")]
    public async Task<IActionResult> GetImage(int id)
    {
        var post = await db.Posts.FindAsync(id);
        return post is null ? NotFound() : Ok(new { post.ImagePath });
    }

    private void DeleteImageFile(string imagePath)
    {
        var fileName = Path.GetFileName(imagePath);
        var fullPath = Path.Combine(env.ContentRootPath, "wwwroot", "uploads", fileName);

        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }
}