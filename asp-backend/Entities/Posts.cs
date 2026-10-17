namespace JustOne.Entities;

public class Post
{
    public int Id { get; set; }

    public string UserId { get; set; } = default!;

    // Relative path/URL to the stored image, e.g. "/uploads/xyz.jpg"
    public string ImagePath { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
}