using JustOne.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JustOne.Tests.Infrastructure;

// Starts the real API in memory against its own in-memory SQLite database.
// Every factory instance (one per test class) gets a fresh, migrated database,
// and uploaded files go to a temporary folder instead of the project's wwwroot.
public class JustOneFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"Data Source=justone-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;

    public string ContentRoot { get; } = Path.Combine(Path.GetTempPath(), $"justone-tests-{Guid.NewGuid():N}");

    public string UploadsFolder => Path.Combine(ContentRoot, "wwwroot", "uploads");

    public JustOneFactory()
    {
        // A shared in-memory database lives as long as at least one connection is open.
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();

        // The schema must exist before Program runs DbSeeder on startup.
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connectionString).Options);
        db.Database.Migrate();

        // The API reads appsettings.json from its content root, so copy it into the temp folder.
        Directory.CreateDirectory(UploadsFolder);
        File.Copy(Path.Combine(ApiProjectFolder(), "appsettings.json"), Path.Combine(ContentRoot, "appsettings.json"));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseContentRoot(ContentRoot);
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _keepAlive.Dispose();
        try
        {
            Directory.Delete(ContentRoot, recursive: true);
        }
        catch (IOException)
        {
            // A file may still be locked on Windows - it's only a temp folder.
        }
    }

    private static string ApiProjectFolder()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "asp-backend", "JustOne.csproj")))
        {
            dir = dir.Parent;
        }

        return dir is null
            ? throw new InvalidOperationException("Could not find the asp-backend folder above the test output.")
            : Path.Combine(dir.FullName, "asp-backend");
    }
}
