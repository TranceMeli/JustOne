using JustOne.Entities;
using Microsoft.AspNetCore.Identity;

namespace JustOne.Data;

// Fills a separate demo database with users, nearly complete calendars (photos, GIFs and texts),
// comments and guestbook entries - for screenshots and for trying the app out.
// Only runs when started with --DemoData:Enabled=true (see README); never touches the real database.
public static class DemoDataSeeder
{
    private const string DemoPassword = "Demo123!";

    private record DemoUser(string Email, string FirstName, string LastName);

    // Anna (anna@justone.com) is created by DbSeeder; these are her friends.
    private static readonly DemoUser[] Friends =
    [
        new("lena@justone.com", "Lena", "Hoffmann"),
        new("max@justone.com", "Max", "Weber"),
        new("sara@justone.com", "Sara", "Kim")
    ];

    private static readonly string[] Texts =
    [
        "Morning fog on the river.\nThe city still asleep,\nonly the bridge and me.",
        "Today I didn't take a photo.\nI just listened to the rain\nand that was enough.",
        "Coffee, a window seat,\nand nowhere to be.",
        "Small things count:\na warm bread roll, a kind word,\nthe last train home.",
        "Autumn writes in red and gold\nand I try to read it\nbefore the wind turns the page.",
        "\"Do one thing every day.\"\nToday it was saying sorry.",
        "Late light on the balcony.\nThe plants and I\nboth leaning towards it.",
        "One day, one moment.\nThe rest can wait."
    ];

    private static readonly string[] Comments =
    [
        "Wow, the light in this one! 😍",
        "Where was this taken?",
        "This could be a postcard.",
        "Love the colours 🔥",
        "That view though!",
        "Need to go there too.",
        "So calm. Perfect start to the day.",
        "Great one 👍",
        "Beautiful words.",
        "This made my day 🎉"
    ];

    private static readonly (string Email, string Message, int DaysAgo)[] GuestbookEntries =
    [
        ("lena@justone.com", "Day 40 of one post a day – I notice so much more on my way to work now.", 0),
        ("max@justone.com", "Missed two days last week, but the calendar still looks great. Back on track! 💪", 1),
        ("anna@justone.com", "Tried a text instead of a photo today. Feels surprisingly good to write a few lines ✍️", 2),
        ("admin@justone.com", "Welcome to JustOne! One post per day – a photo, a GIF or a few words. Say hi here 👋", 3),
        ("sara@justone.com", "Tip: post in the morning, then the rest of the day is free.", 4),
        ("lena@justone.com", "Does anyone else plan their walks around the daily photo now? 😄", 8),
        ("max@justone.com", "Hello everyone! New here.", 12)
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DemoData");

        // Already seeded? Then leave the demo database as it is.
        if (await userManager.FindByEmailAsync(Friends[0].Email) is not null)
        {
            return;
        }

        var seedFolder = Path.Combine(env.ContentRootPath, "SeedData");
        var photos = Files(Path.Combine(seedFolder, "photos"), "*.jpg");
        var gifs = Files(Path.Combine(seedFolder, "gifs"), "*.gif");
        if (photos.Length == 0)
        {
            logger.LogWarning("No demo photos found in {Folder} - demo data skipped.", seedFolder);
            return;
        }

        var uploads = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploads);

        // Everyone who posts: Anna plus her friends. The admin only writes in the guestbook.
        var posters = new List<ApplicationUser>();
        var anna = await userManager.FindByEmailAsync("anna@justone.com");
        if (anna is not null)
        {
            posters.Add(anna);
        }

        foreach (var friend in Friends)
        {
            var user = new ApplicationUser
            {
                FirstName = friend.FirstName,
                LastName = friend.LastName,
                UserName = friend.Email,
                Email = friend.Email,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, DemoPassword);
            await userManager.AddToRoleAsync(user, Roles.User);
            posters.Add(user);
        }

        var random = new Random(42);
        var today = DateTime.UtcNow.Date;
        var previousMonthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1);
        int photoIndex = 0, gifIndex = 0, textIndex = 0;

        string CopyToUploads(string source)
        {
            var fileName = $"demo-{Guid.NewGuid()}{Path.GetExtension(source)}";
            File.Copy(source, Path.Combine(uploads, fileName));
            return $"/uploads/{fileName}";
        }

        // Previous month and the current month up to yesterday, with only 2-3 gaps per user.
        // Today stays open, so the "Today" card shows its posting state.
        foreach (var user in posters)
        {
            var missed = new HashSet<DateTime>();
            while (missed.Count < 3)
            {
                missed.Add(previousMonthStart.AddDays(random.Next((today - previousMonthStart).Days)));
            }

            var posts = new List<Post>();
            var n = 0;
            for (var day = previousMonthStart; day < today; day = day.AddDays(1))
            {
                if (missed.Contains(day))
                {
                    continue;
                }

                // Mostly photos, every 5th day a text, every 7th a GIF.
                var post = new Post { UserId = user.Id, CreatedAt = day };
                if (n % 5 == 2)
                {
                    post.Text = Texts[textIndex++ % Texts.Length];
                }
                else if (n % 7 == 4 && gifs.Length > 0)
                {
                    post.ImagePath = CopyToUploads(gifs[gifIndex++ % gifs.Length]);
                }
                else
                {
                    post.ImagePath = CopyToUploads(photos[photoIndex++ % photos.Length]);
                }

                posts.Add(post);
                n++;
            }

            db.Posts.AddRange(posts);
            await db.SaveChangesAsync();

            // Comments from the others on about every second post.
            var others = posters.Where(u => u.Id != user.Id).ToArray();
            foreach (var post in posts.Where((_, i) => i % 2 == 0))
            {
                var count = random.Next(1, 4);
                for (var c = 0; c < count; c++)
                {
                    db.Comments.Add(new Comment
                    {
                        PostId = post.Id,
                        UserId = others[random.Next(others.Length)].Id,
                        Message = Comments[random.Next(Comments.Length)],
                        CreatedAt = post.CreatedAt.AddHours(9 + c * 3 + random.Next(3)).AddMinutes(random.Next(60))
                    });
                }
            }
        }

        foreach (var (email, message, daysAgo) in GuestbookEntries)
        {
            var author = await userManager.FindByEmailAsync(email);
            if (author is not null)
            {
                db.GuestbookEntries.Add(new GuestbookEntry
                {
                    UserId = author.Id,
                    Message = message,
                    CreatedAt = today.AddDays(-daysAgo).AddHours(8 + daysAgo % 10).AddMinutes(17 * daysAgo % 60)
                });
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Demo data created for {Count} users.", posters.Count);
    }

    private static string[] Files(string folder, string pattern) =>
        Directory.Exists(folder) ? Directory.GetFiles(folder, pattern).Order().ToArray() : [];
}
