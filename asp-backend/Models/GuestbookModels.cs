using System.ComponentModel.DataAnnotations;

namespace JustOne.Models;

public record CreateGuestbookEntryRequest([StringLength(2000)] string Message);

public record GuestbookEntryResponse(int Id, string Message, DateTime CreatedAt);