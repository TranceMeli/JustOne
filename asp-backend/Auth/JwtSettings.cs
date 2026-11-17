namespace JustOne.Auth
{
    public class JwtSettings
    {

        public string Key { get; set; } = string.Empty;
        public string Issuer {  get; set; } = string.Empty;
        public string Audience {  get; set; } = string.Empty;
        // The access token is short-lived; the refresh token lives longer so users stay logged in.
        public int ExpiryMinutes { get; set; }
        public int RefreshTokenDays { get; set; }
    }
}
