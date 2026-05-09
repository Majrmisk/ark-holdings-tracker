using Application.Settings;
using Domain;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var adminSettings = serviceProvider.GetRequiredService<IOptions<AdminUserSettings>>().Value;

        foreach (var role in Enum.GetValues<UserRole>())
        {
            var roleName = role.ToString();
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        var adminUsername = adminSettings.Username;
        var adminPassword = adminSettings.Password;

        if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException("Admin user seed settings must include Username and Password.");
        }

        if (await userManager.FindByNameAsync(adminUsername) is null)
        {
            var admin = new AppUser
            {
                UserName = adminUsername,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, nameof(UserRole.SuperAdmin));
            }
        }
    }
}
