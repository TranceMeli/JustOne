namespace JustOne.Models;

public static class PostModels
{
    // ImageUrl is set for photo/GIF posts, Text for text posts.
    public record PostResponse(int Id, DateTime CreatedAt, string? ImageUrl, string? Text);

    public record CalendarDayResponse(int Day, bool HasPost, string? ImageUrl, int? PostId, string? Text);

    public record CalendarResponse(int Year, int Month, List<CalendarDayResponse> Days);
}
