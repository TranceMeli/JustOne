namespace JustOne.Models
{
    public class AuthModels
    {

        public record RegisterRequest(string FirstName, string LastName, string Email, string Password);

        public record LoginRequest(string Email, string Password);

        public record RefreshRequest(string RefreshToken);

        public record RevokeRequest(string RefreshToken);

        public record AddRoleRequest(string Email, string Role);


        public record AuthResponse(
            string UserId,
            string Email,
            IEnumerable<string> Roles,
            string AccessToken,
            DateTime AccessExpiresAt,
            string RefreshToken,
            DateTime RefreshTokenExpiresAt);
    }
}
