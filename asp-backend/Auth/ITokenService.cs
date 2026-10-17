using JustOne.Entities;

namespace JustOne.Auth;

public interface ITokenService
{
    (string AccessToken, DateTime ExpiresAt) CreateAccessToken(ApplicationUser user, IEnumerable<string> roles);

    string CreateRefreshToken();
}
