using System.Net;
using System.Net.Http.Json;
using JustOne.Models;
using JustOne.Tests.Infrastructure;

namespace JustOne.Tests;

public class AuthTests(JustOneFactory factory) : IClassFixture<JustOneFactory>
{
    [Fact]
    public async Task Register_then_login_returns_tokens_and_user_role()
    {
        var client = factory.CreateClient();
        var email = $"new-{Guid.NewGuid():N}@test.local";

        var register = await ApiHelpers.RegisterAsync(client, email, "Mia", "Lang");
        var login = await ApiHelpers.LoginAsync(client, email);

        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        Assert.Equal(email, login.Email);
        Assert.Equal("Mia", login.FirstName);
        Assert.Equal(["User"], login.Roles);
        Assert.False(string.IsNullOrEmpty(login.AccessToken));
        Assert.False(string.IsNullOrEmpty(login.RefreshToken));
    }

    [Fact]
    public async Task Register_with_an_existing_email_is_rejected()
    {
        var client = factory.CreateClient();
        var email = $"twice-{Guid.NewGuid():N}@test.local";
        await ApiHelpers.RegisterAsync(client, email);

        var second = await ApiHelpers.RegisterAsync(client, email);

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Register_with_a_weak_password_returns_the_reasons()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new AuthModels.RegisterRequest("Weak", "Pass", $"weak-{Guid.NewGuid():N}@test.local", "abc"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var reasons = await response.Content.ReadFromJsonAsync<string[]>();
        Assert.NotEmpty(reasons!);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_returns_401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new AuthModels.LoginRequest(ApiHelpers.DemoEmail, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Seeded_accounts_can_log_in_with_their_roles()
    {
        var client = factory.CreateClient();

        var admin = await ApiHelpers.LoginAsync(client, ApiHelpers.AdminEmail, ApiHelpers.AdminPassword);
        var demo = await ApiHelpers.LoginAsync(client, ApiHelpers.DemoEmail, ApiHelpers.DemoPassword);

        Assert.Contains("Admin", admin.Roles);
        Assert.Equal(["User"], demo.Roles);
        Assert.Equal("Anna", demo.FirstName);
    }

    [Fact]
    public async Task Protected_endpoints_require_a_token()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/guestbook");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_the_refresh_token()
    {
        var (client, login) = await ApiHelpers.NewUserAsync(factory);

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new AuthModels.RefreshRequest(login.RefreshToken));
        var refreshed = await response.Content.ReadFromJsonAsync<LoginResult>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(login.RefreshToken, refreshed!.RefreshToken);
    }

    [Fact]
    public async Task Reusing_a_rotated_refresh_token_revokes_all_sessions()
    {
        var (client, login) = await ApiHelpers.NewUserAsync(factory);
        var first = await client.PostAsJsonAsync("/api/auth/refresh", new AuthModels.RefreshRequest(login.RefreshToken));
        var newToken = (await first.Content.ReadFromJsonAsync<LoginResult>())!.RefreshToken;

        // The old token shows up again -> treated as stolen.
        var reuse = await client.PostAsJsonAsync("/api/auth/refresh", new AuthModels.RefreshRequest(login.RefreshToken));
        var newTokenAfterTheft = await client.PostAsJsonAsync("/api/auth/refresh", new AuthModels.RefreshRequest(newToken));

        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, newTokenAfterTheft.StatusCode);
    }

    [Fact]
    public async Task Revoked_refresh_token_can_no_longer_be_used()
    {
        var (client, login) = await ApiHelpers.NewUserAsync(factory);

        var revoke = await client.PostAsJsonAsync("/api/auth/revoke", new AuthModels.RevokeRequest(login.RefreshToken));
        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new AuthModels.RefreshRequest(login.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Only_admins_can_assign_roles()
    {
        var (userClient, _) = await ApiHelpers.NewUserAsync(factory);
        var (_, target) = await ApiHelpers.NewUserAsync(factory, "Target");
        var adminClient = await ApiHelpers.LoginClientAsync(factory, ApiHelpers.AdminEmail, ApiHelpers.AdminPassword);
        var request = new AuthModels.AddRoleRequest(target.Email, "Manager");

        var asUser = await userClient.PostAsJsonAsync("/api/auth/add-role", request);
        var asAdmin = await adminClient.PostAsJsonAsync("/api/auth/add-role", request);
        var twice = await adminClient.PostAsJsonAsync("/api/auth/add-role", request);

        Assert.Equal(HttpStatusCode.Forbidden, asUser.StatusCode);
        Assert.Equal(HttpStatusCode.OK, asAdmin.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, twice.StatusCode);
    }
}
