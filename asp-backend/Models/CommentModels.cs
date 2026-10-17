namespace JustOne.Models;

public record CreateCommentRequest(string Message);

public record CommentResponse(int Id, string Message, DateTime CreatedAt, string AuthorEmail);