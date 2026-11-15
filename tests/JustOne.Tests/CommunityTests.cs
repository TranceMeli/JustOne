using System.Net;
using System.Net.Http.Json;
using JustOne.Models;
using JustOne.Tests.Infrastructure;
using static JustOne.Models.PostModels;

namespace JustOne.Tests;

// Comments and the public guestbook: what other users can see and how authors are shown.
public class CommunityTests(JustOneFactory factory) : IClassFixture<JustOneFactory>
{
    private async Task<int> CreatePostAsync(HttpClient client) =>
        (await (await client.PostAsync("/api/posts", ApiHelpers.TextForm("A post to talk about"))).Content.ReadFromJsonAsync<PostResponse>())!.Id;

    [Fact]
    public async Task Comments_show_the_author_name_and_never_the_email()
    {
        var (owner, _) = await ApiHelpers.NewUserAsync(factory, "Olivia", "Stone");
        var (friend, friendLogin) = await ApiHelpers.NewUserAsync(factory, "Finn", "Berger");
        var postId = await CreatePostAsync(owner);

        var created = await friend.PostAsJsonAsync($"/api/posts/{postId}/comments", new CreateCommentRequest("  Lovely!  "));
        var asOwner = await owner.GetFromJsonAsync<CommentResponse[]>($"/api/posts/{postId}/comments");
        var asFriend = await friend.GetFromJsonAsync<CommentResponse[]>($"/api/posts/{postId}/comments");
        var json = await owner.GetStringAsync($"/api/posts/{postId}/comments");

        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var comment = Assert.Single(asOwner!);
        Assert.Equal("Lovely!", comment.Message);
        Assert.Equal("Finn B.", comment.AuthorName);
        Assert.False(comment.IsOwn);
        Assert.True(Assert.Single(asFriend!).IsOwn);
        Assert.DoesNotContain(friendLogin.Email, json);
    }

    [Fact]
    public async Task Comments_are_returned_oldest_first()
    {
        var (client, _) = await ApiHelpers.NewUserAsync(factory);
        var postId = await CreatePostAsync(client);

        foreach (var message in new[] { "first", "second", "third" })
        {
            await client.PostAsJsonAsync($"/api/posts/{postId}/comments", new CreateCommentRequest(message));
        }
        var comments = await client.GetFromJsonAsync<CommentResponse[]>($"/api/posts/{postId}/comments");

        Assert.Equal(["first", "second", "third"], comments!.Select(c => c.Message));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_comments_are_rejected(string message)
    {
        var (client, _) = await ApiHelpers.NewUserAsync(factory);
        var postId = await CreatePostAsync(client);

        var response = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", new CreateCommentRequest(message));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Comments_longer_than_1000_characters_are_rejected()
    {
        var (client, _) = await ApiHelpers.NewUserAsync(factory);
        var postId = await CreatePostAsync(client);

        var response = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", new CreateCommentRequest(new string('x', 1001)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Comments_on_a_missing_post_return_404()
    {
        var (client, _) = await ApiHelpers.NewUserAsync(factory);

        var get = await client.GetAsync("/api/posts/999999/comments");
        var post = await client.PostAsJsonAsync("/api/posts/999999/comments", new CreateCommentRequest("hello?"));

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
    }

    [Fact]
    public async Task Guestbook_is_public_and_marks_own_entries()
    {
        var (writer, writerLogin) = await ApiHelpers.NewUserAsync(factory, "Greta", "Wolf");
        var (reader, _) = await ApiHelpers.NewUserAsync(factory, "Ron");
        var message = $"Hello from Greta {Guid.NewGuid():N}";

        var created = await writer.PostAsJsonAsync("/api/guestbook", new CreateGuestbookEntryRequest(message));
        var asReader = await reader.GetFromJsonAsync<GuestbookEntryResponse[]>("/api/guestbook");
        var asWriter = await writer.GetFromJsonAsync<GuestbookEntryResponse[]>("/api/guestbook");
        var json = await reader.GetStringAsync("/api/guestbook");

        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var seenByReader = asReader!.Single(e => e.Message == message);
        Assert.Equal("Greta W.", seenByReader.AuthorName);
        Assert.False(seenByReader.IsOwn);
        Assert.True(asWriter!.Single(e => e.Message == message).IsOwn);
        Assert.DoesNotContain(writerLogin.Email, json);
    }

    [Fact]
    public async Task Guestbook_lists_the_newest_entries_first()
    {
        var (client, _) = await ApiHelpers.NewUserAsync(factory);
        var older = $"older {Guid.NewGuid():N}";
        var newer = $"newer {Guid.NewGuid():N}";

        await client.PostAsJsonAsync("/api/guestbook", new CreateGuestbookEntryRequest(older));
        await Task.Delay(20);
        await client.PostAsJsonAsync("/api/guestbook", new CreateGuestbookEntryRequest(newer));
        var entries = (await client.GetFromJsonAsync<GuestbookEntryResponse[]>("/api/guestbook"))!.Select(e => e.Message).ToList();

        Assert.True(entries.IndexOf(newer) < entries.IndexOf(older));
    }

    [Fact]
    public async Task Guestbook_rejects_empty_and_too_long_entries()
    {
        var (client, _) = await ApiHelpers.NewUserAsync(factory);

        var empty = await client.PostAsJsonAsync("/api/guestbook", new CreateGuestbookEntryRequest(" "));
        var tooLong = await client.PostAsJsonAsync("/api/guestbook", new CreateGuestbookEntryRequest(new string('x', 2001)));

        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
    }
}
