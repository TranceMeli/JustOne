using System.IdentityModel.Tokens.Jwt;
using JustOne.Auth;
using JustOne.Entities;
using JustOne.Models;
using Microsoft.Extensions.Options;

namespace JustOne.Tests;

// Small, isolated pieces that don't need the web server.
public class UserNamesTests
{
    [Theory]
    [InlineData("Anna", "Berg", "anna@justone.com", "Anna B.")]
    [InlineData("JustOne Team", "", "admin@justone.com", "JustOne Team")]
    [InlineData("", "Berg", "anna@justone.com", "anna")]
    [InlineData("  ", "", null, "Someone")]
    public void Display_name_never_reveals_the_email(string first, string last, string? email, string expected)
    {
        Assert.Equal(expected, UserNames.Display(first, last, email));
    }
}

public class TokenServiceTests
{
    private static readonly JwtSettings Settings = new()
    {
        Key = "unit-test-signing-key-that-is-long-enough-for-hmac-sha256",
        Issuer = "JustOne-tests",
        Audience = "JustOne-tests-api",
        ExpiryMinutes = 15,
        RefreshTokenDays = 7
    };

    private readonly TokenService _service = new(Options.Create(Settings));

    [Fact]
    public void Access_token_contains_user_claims_and_roles()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "anna@justone.com", FirstName = "Anna", LastName = "Berg" };

        var (token, expiresAt) = _service.CreateAccessToken(user, ["User", "Manager"]);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("user-1", jwt.Subject);
        Assert.Equal("Anna Berg", jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Name).Value);
        Assert.Equal(["User", "Manager"], jwt.Claims.Where(c => c.Type == "role").Select(c => c.Value));
        Assert.Equal("JustOne-tests", jwt.Issuer);
        Assert.InRange(expiresAt, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
    }

    [Fact]
    public void Refresh_tokens_are_random_and_long()
    {
        var a = _service.CreateRefreshToken();
        var b = _service.CreateRefreshToken();

        Assert.NotEqual(a, b);
        Assert.Equal(64, Convert.FromBase64String(a).Length);
    }
}

public class RefreshTokenTests
{
    [Fact]
    public void Token_is_active_only_until_it_expires_or_is_revoked()
    {
        var active = new RefreshToken { Expires = DateTime.UtcNow.AddDays(1) };
        var expired = new RefreshToken { Expires = DateTime.UtcNow.AddSeconds(-1) };
        var revoked = new RefreshToken { Expires = DateTime.UtcNow.AddDays(1), Revoked = DateTime.UtcNow };

        Assert.True(active.IsActive);
        Assert.False(expired.IsActive);
        Assert.False(revoked.IsActive);
    }
}
