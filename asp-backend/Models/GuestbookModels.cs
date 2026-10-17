namespace JustOne.Models;

public record CreateGuestbookEntryRequest(string Message);

public record GuestbookEntryResponse(int Id, string Message, DateTime CreatedAt);