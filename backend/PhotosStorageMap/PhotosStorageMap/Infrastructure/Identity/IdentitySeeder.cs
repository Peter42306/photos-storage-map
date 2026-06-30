using Microsoft.AspNetCore.Identity;

namespace PhotosStorageMap.Infrastructure.Identity
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var configuration = services.GetRequiredService<IConfiguration>();

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            await EnsureRoleAsync(roleManager, RoleNames.Admin);
            await EnsureRoleAsync(roleManager, RoleNames.User);            

            var adminEmail = configuration["Admin:Email"];

            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                return;
            }

            var adminUser = await userManager.FindByNameAsync(adminEmail);

            if (adminUser is null)
            {
                return;
            }

            if (!await userManager.IsInRoleAsync(adminUser, RoleNames.Admin))
            {
                await userManager.AddToRoleAsync(adminUser, RoleNames.Admin);
            }
        }

        private static async Task EnsureRoleAsync(
            RoleManager<IdentityRole> roleManager,
            string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
}
