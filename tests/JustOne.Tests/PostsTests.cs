using System.Net;
using System.Net.Http.Json;
using JustOne.Tests.Infrastructure;
using static JustOne.Models.PostModels;

namespace JustOne.Tests;

public class PostsTests(JustOneFactory factory) : IClassFixture<JustOneFactory>
{
    private static readonly DateTime UtcToday = DateTime.UtcNow.Date;

    private static async Task<CalendarResponse> CalendarAsync(HttpClient client, DateTime day) =>
        (await client.GetFromJsonAsync<CalendarResponse>($"/api/posts/calendar?year={day.Year}&month={day.Month}"))!;

    [Fact]
    public async Task Image_post_shows_up_in_the_calendar_and_is_stored()
    {
        var (client, _) = await ApiHelpers.NewUserAsync(factory);

        var response = await client.PostAsync("/api/posts", ApiHelpers.ImageForm());
        var post = await response.Content.ReadFromJsonAsync<PostResponse>();
        var calendar = await CalendarAsync(client, post!.CreatedAt);
        var day = calendar.Days.Single(d => d.Day == post.CreatedAt.Day);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("/uploads/", post.ImageUrl);
        Assert.Null(post.Text);
        Assert.True(day.HasPost);
        Assert.Equal(post.Id, day.PostId);
        Assert.True(File.Exists(Path.Combine(factory.UploadsFolder, Path.GetFileName(post.ImageUrl!))));
    }

    [Fact]
    public async Task Text_post_is_trimmed_and_shows_up_in_the_calendar()
    {
        var (client, _) = await ApiHelpers.NewUserAsync(factory);

        var response = await client.PostAsync("/api/posts", ApiHelpers.TextForm("  Morning fog.\nOnly the bridge and me.  "));
        var post = await response.Content.ReadFromJsonAsync<PostResponse>();
        var calendar = await CalendarAsync(client, post!.CreatedAt);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(post.ImageUrl);
        Assert.Equal("Morning fog.\nOnly the bridge and me.", post.Text);
        Assert.Equal(post.Text, calendar.Days.Single(d => d.Day == post.CreatedAt.Day).Text);
    }

    [Fact]
    public async Task Only_one_post_per_day_is_allowed()
    {
        var (client, _) = await ApiHelpers.NewUserAsync(factory);
        await client.PostAsync("/api/posts", ApiHelpers.TextForm("first"));

        var second = await client.PostAsync("/api/posts", ApiHelpers.ImageForm());

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Theory]
    [InlineData("both")]
    [InlineData("none")]
    [InlineData("blank-text")]
    [InlineData("too-long-text")]
    [InlineData("html-file")]
    [InlineData("empty-file")]
    public async Task Invalid_posts_are_rejected(string kind)
    {
        var (client, _) = await ApiHelpers.NewUserAsync(factory);
        var form = kind switch
        {
            "both" => ApiHelpers.ImageForm(),
            "none" => new MultipartFormDataContent { { new StringContent(""), "other" } },
            "blank-text" => ApiHelpers.TextForm("    "),
            "too-long-text" => ApiHelpers.TextForm(new string('a', 501)),
            "html-file" => ApiHelpers.ImageForm("evil.html"),
            _ => ApiHelpers.ImageForm("empty.png", [])
        };
        if (kind == "both")
        {
            form.Add(new StringContent("and a text"), "text");
        }

        var response = await client.PostAsync("/api/posts", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Changing_an_image_post_to_text_deletes_the_old_file()
    {
        var (client, _) = await ApiHelpers.NewUserAsync(factory);
        var created = await (await client.PostAsync("/api/posts", ApiHelpers.ImageForm())).Content.ReadFromJsonAsync<PostResponse>();
        var oldFile = Path.Combine(factory.UploadsFolder, Path.GetFileName(created!.ImageUrl!));

        var response = await client.PutAsync("/api/posts", ApiHelpers.TextForm("Changed my mind."));
        var updated = await response.Content.ReadFromJsonAsync<PostResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Null(updated.ImageUrl);
        Assert.Equal("Changed my mind.", updated.Text);
        Assert.False(File.Exists(oldFile));
    }

    [Fact]
    public async Task Update_without_a_post_today_returns_404()
    {
        var (client, _) = await ApiHelpers.NewUserAsync(factory);

        var response = await client.PutAsync("/api/posts", ApiHelpers.TextForm("nothing to change"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_removes_todays_post_and_allows_a_new_one()
    {
        var (client, _) = await ApiHelpers.NewUserAsync(factory);
        await client.PostAsync("/api/posts", ApiHelpers.TextForm("short-lived"));

        var delete = await client.DeleteAsync("/api/posts");
        var deleteAgain = await client.DeleteAsync("/api/posts");
        var newPost = await client.PostAsync("/api/posts", ApiHelpers.TextForm("second try"));

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteAgain.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newPost.StatusCode);
    }

    [Fact]
    public async Task Today_is_taken_from_the_users_time_zone()
    {
        // UTC+14 and UTC-12 are always on different calendar days, so both posts are allowed.
        var (client, _) = await ApiHelpers.NewUserAsync(factory);

        var east = await client.SendAsync(ApiHelpers.WithTimeZone(HttpMethod.Post, "/api/posts", ApiHelpers.TextForm("east"), "Pacific/Kiritimati"));
        var west = await client.SendAsync(ApiHelpers.WithTimeZone(HttpMethod.Post, "/api/posts", ApiHelpers.TextForm("west"), "Etc/GMT+12"));
        var eastPost = await east.Content.ReadFromJsonAsync<PostResponse>();
        var westPost = await west.Content.ReadFromJsonAsync<PostResponse>();

        Assert.Equal(HttpStatusCode.OK, east.StatusCode);
        Assert.Equal(HttpStatusCode.OK, west.StatusCode);
        // 26 hours apart: depending on the time of day that's one or two calendar days.
        Assert.InRange((eastPost!.CreatedAt - westPost!.CreatedAt).TotalDays, 1, 2);
    }

    [Fact]
    public async Task Unknown_time_zone_falls_back_to_utc()
    {
        var (client, _) = await ApiHelpers.NewUserAsync(factory);

        var response = await client.SendAsync(ApiHelpers.WithTimeZone(HttpMethod.Post, "/api/posts", ApiHelpers.TextForm("utc"), "Not/AZone"));
        var post = await response.Content.ReadFromJsonAsync<PostResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(DateTime.UtcNow.Date, post!.CreatedAt.Date);
    }

    [Theory]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    [InlineData(0, 5)]
    public async Task Calendar_rejects_invalid_months(int year, int month)
    {
        var (client, _) = await ApiHelpers.NewUserAsync(factory);

        var response = await client.GetAsync($"/api/posts/calendar?year={year}&month={month}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Calendar_has_one_entry_per_day_of_the_month()
    {
        var (client, _) = await ApiHelpers.NewUserAsync(factory);

        var february = await client.GetFromJsonAsync<CalendarResponse>("/api/posts/calendar?year=2028&month=2");

        Assert.Equal(29, february!.Days.Count);
        Assert.All(february.Days, d => Assert.False(d.HasPost));
    }

    [Fact]
    public async Task Calendar_only_shows_the_users_own_posts()
    {
        var (alice, _) = await ApiHelpers.NewUserAsync(factory, "Alice");
        var (bob, _) = await ApiHelpers.NewUserAsync(factory, "Bob");
        await alice.PostAsync("/api/posts", ApiHelpers.TextForm("Alice's day"));

        var bobsCalendar = await CalendarAsync(bob, UtcToday);

        Assert.DoesNotContain(bobsCalendar.Days, d => d.HasPost);
    }

    [Fact]
    public async Task Moderation_is_limited_to_admins_and_managers()
    {
        var (owner, _) = await ApiHelpers.NewUserAsync(factory, "Owner");
        var (stranger, _) = await ApiHelpers.NewUserAsync(factory, "Stranger");
        var admin = await ApiHelpers.LoginClientAsync(factory, ApiHelpers.AdminEmail, ApiHelpers.AdminPassword);
        var post = await (await owner.PostAsync("/api/posts", ApiHelpers.ImageForm())).Content.ReadFromJsonAsync<PostResponse>();

        var byStranger = await stranger.DeleteAsync($"/api/posts/{post!.Id}");
        var byAdmin = await admin.DeleteAsync($"/api/posts/{post.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, byStranger.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byAdmin.StatusCode);
    }
}
