using System.Net.Http.Headers;
using System.Net.Http.Json;
using JustOne.Models;

namespace JustOne.Tests.Infrastructure;

public record LoginResult(string UserId, string Email, string FirstName, string[] Roles, string AccessToken, string RefreshToken);

public static class ApiHelpers
{
    // Seeded by DbSeeder on startup.
    public const string AdminEmail = "admin@justone.com";
    public const string AdminPassword = "Admin123!";
    public const string DemoEmail = "anna@justone.com";
    public const string DemoPassword = "Demo123!";

    public const string Password = "Test123!";

    // A valid 1x1 PNG - enough for the upload endpoints.
    public static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    public static async Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string firstName = "Test", string lastName = "User") =>
        await client.PostAsJsonAsync("/api/auth/register", new AuthModels.RegisterRequest(firstName, lastName, email, Password));

    public static async Task<LoginResult> LoginAsync(HttpClient client, string email, string password = Password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new AuthModels.LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResult>())!;
    }

    // Registers a fresh user (unique email) and returns a client that sends its access token.
    public static async Task<(HttpClient Client, LoginResult Login)> NewUserAsync(JustOneFactory factory, string firstName = "Test", string lastName = "User")
    {
        var client = factory.CreateClient();
        var email = $"{firstName.ToLowerInvariant()}-{Guid.NewGuid():N}@test.local";
        (await RegisterAsync(client, email, firstName, lastName)).EnsureSuccessStatusCode();
        var login = await LoginAsync(client, email);
        return (Authorize(client, login.AccessToken), login);
    }

    public static async Task<HttpClient> LoginClientAsync(JustOneFactory factory, string email, string password)
    {
        var client = factory.CreateClient();
        var login = await LoginAsync(client, email, password);
        return Authorize(client, login.AccessToken);
    }

    public static HttpClient Authorize(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    public static MultipartFormDataContent ImageForm(string fileName = "photo.png", byte[]? bytes = null)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes ?? TinyPng);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", fileName);
        return form;
    }

    public static MultipartFormDataContent TextForm(string text) => new() { { new StringContent(text), "text" } };

    public static HttpRequestMessage WithTimeZone(HttpMethod method, string url, HttpContent? content, string timeZone) =>
        new(method, url) { Content = content, Headers = { { "X-Timezone", timeZone } } };
}
