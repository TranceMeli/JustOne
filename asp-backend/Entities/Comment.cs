namespace JustOne.Entities;

public class Comment
{
    public int Id { get; set; }

    public int PostId { get; set; }

    public string UserId { get; set; } = default!;

    public string Message { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Post Post { get; set; } = default!;
}