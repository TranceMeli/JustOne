using JustOne.Entities;
using Microsoft.AspNetCore.Identity;

namespace JustOne.Data
{
    public static class DbSeeder
    {
        // Admin for moderation, plus a regular user for the "Try the demo account" button.
        private static readonly (string Email, string Password, string FirstName, string LastName, string Role)[] Accounts =
        [
            ("admin@justone.com", "Admin123!", "JustOne Team", "", Roles.Admin),
            ("anna@justone.com", "Demo123!", "Anna", "Berg", Roles.User)
        ];

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var role in new[] { Roles.Admin, Roles.Manager, Roles.User })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            foreach (var (email, password, firstName, lastName, role) in Accounts)
            {
                if (await userManager.FindByEmailAsync(email) is not null)
                {
                    continue;
                }

                var user = new ApplicationUser
                {
                    FirstName = firstName,
                    LastName = lastName,
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(user, password);
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
