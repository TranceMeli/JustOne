namespace JustOne.Auth
{
    public class JwtSettings
    {

        public string Key { get; set; } = string.Empty;
        public string Issuer {  get; set; } = string.Empty;
        public string Audience {  get; set; } = string.Empty;
        // access tokens short-lived, refresh longer
        public int ExpiryMinutes { get; set; }
        public int RefreshTokenDays { get; set; }
    }
}
