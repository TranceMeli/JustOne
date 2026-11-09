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
[Route("api/posts/{postId:int}/comments")]
[Authorize]
public class CommentsController(AppDbContext db) : ControllerBase
{
    private string UserId => User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

    [HttpGet]
    public async Task<IActionResult> GetComments(int postId)
    {
        var postExists = await db.Posts.AnyAsync(p => p.Id == postId);
        if (!postExists)
        {
            return NotFound("Post not found.");
        }

        // Join against the Identity user table to show the author of each comment.
        var rows = await db.Comments
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreatedAt)
            .Join(db.Users, c => c.UserId, u => u.Id,
                (c, u) => new { c.Id, c.Message, c.CreatedAt, c.UserId, u.FirstName, u.LastName, u.Email })
            .ToListAsync();

        var comments = rows.Select(r => new CommentResponse(
            r.Id, r.Message, r.CreatedAt, UserNames.Display(r.FirstName, r.LastName, r.Email), r.UserId == UserId));

        return Ok(comments);
    }

    [HttpPost]
    public async Task<IActionResult> CreateComment(int postId, CreateCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message must not be empty.");
        }

        var postExists = await db.Posts.AnyAsync(p => p.Id == postId);
        if (!postExists)
        {
            return NotFound("Post not found.");
        }

        var author = await db.Users.FindAsync(UserId);
        if (author is null)
        {
            return Unauthorized();
        }

        var comment = new Comment
        {
            PostId = postId,
            UserId = UserId,
            Message = request.Message.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        return Ok(new CommentResponse(
            comment.Id, comment.Message, comment.CreatedAt, UserNames.Display(author.FirstName, author.LastName, author.Email), true));
    }
}