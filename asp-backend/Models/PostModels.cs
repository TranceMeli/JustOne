namespace JustOne.Models;

public static class PostModels
{
    public record PostResponse(int Id, DateTime CreatedAt, string ImageUrl);

    public record CalendarDayResponse(int Day, bool HasPost, string? ImageUrl, int? PostId);

    public record CalendarResponse(int Year, int Month, List<CalendarDayResponse> Days);
}