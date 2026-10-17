namespace JustOne.Entities;

public class RefreshToken
{
    public int Id { get; set; }

    public string Token { get; set; } = default!;

    public string UserId { get; set; } = default!;

    public DateTime Created { get; set; }

    public DateTime Expires { get; set; }

    public DateTime? Revoked { get; set; }

    // Points to the token that replaced this one during rotation.
    public string? ReplacedToken { get; set; }

    // Active = not yet revoked AND not yet expired.
    public bool IsActive => Revoked is null && DateTime.UtcNow < Expires;
}