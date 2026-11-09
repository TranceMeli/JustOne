namespace JustOne.Entities;

// One post per user and day: either an image (photo or GIF) or a short text (poem, thought, quote).
public class Post
{
    public int Id { get; set; }

    public string UserId { get; set; } = default!;

    // Relative path/URL to the stored image, e.g. "/uploads/xyz.jpg" - null for text posts
    public string? ImagePath { get; set; }

    // The text of a text post - null for image posts
    public string? Text { get; set; }

    public DateTime CreatedAt { get; set; }
}
