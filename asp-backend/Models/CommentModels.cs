using System.ComponentModel.DataAnnotations;

namespace JustOne.Models;

public record CreateCommentRequest([StringLength(1000)] string Message);

public record CommentResponse(int Id, string Message, DateTime CreatedAt, string AuthorEmail);