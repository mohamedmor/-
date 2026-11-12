using LivestockMarket.Models;
using LivestockMarket.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LivestockMarket.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var config = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

            try
            {
                if ((await db.Database.GetPendingMigrationsAsync()).Any())
                    await db.Database.MigrateAsync();

                foreach (var role in new[] { Roles.Admin, Roles.User })
                {
                    if (!await roleManager.RoleExistsAsync(role))
                        await roleManager.CreateAsync(new IdentityRole(role));
                }

                if (!await db.Categories.AnyAsync())
                {
                    db.Categories.AddRange(
                        new Category { Name = "أبقار",   Slug = "cows",    Section = Section.Livestock, SortOrder = 1 },
                        new Category { Name = "جاموس",   Slug = "buffalo", Section = Section.Livestock, SortOrder = 2 },
                        new Category { Name = "أغنام",   Slug = "sheep",   Section = Section.Livestock, SortOrder = 3 },
                        new Category { Name = "ماعز",    Slug = "goats",   Section = Section.Livestock, SortOrder = 4 },
                        new Category { Name = "كلاب",    Slug = "dogs",    Section = Section.Pets, SortOrder = 5 },
                        new Category { Name = "قطط",     Slug = "cats",    Section = Section.Pets, SortOrder = 6 },
                        new Category { Name = "طيور",    Slug = "birds",   Section = Section.Pets, SortOrder = 7 },
                        new Category { Name = "أخرى",    Slug = "other",   Section = Section.Other, SortOrder = 99 });
                }

                if (!await db.AnimalConditions.AnyAsync())
                {
                    db.AnimalConditions.AddRange(
                        new AnimalCondition { Name = "بكر",   SortOrder = 1 },
                        new AnimalCondition { Name = "عشر",   SortOrder = 2 },
                        new AnimalCondition { Name = "والدة", SortOrder = 3 },
                        new AnimalCondition { Name = "دكر",   SortOrder = 4 },
                        new AnimalCondition { Name = "نتاية", SortOrder = 5 },
                        new AnimalCondition { Name = "للذبح (لحم)", SortOrder = 6 });
                }

                await db.SaveChangesAsync();

                // The admin account comes from configuration (user-secrets or environment), never from code
                var adminEmail = config["Seed:AdminEmail"];
                var adminPassword = config["Seed:AdminPassword"];
                if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
                {
                    var admin = await userManager.FindByEmailAsync(adminEmail);
                    if (admin == null)
                    {
                        admin = new ApplicationUser
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            EmailConfirmed = true,
                            FullName = "Admin"
                        };
                        var created = await userManager.CreateAsync(admin, adminPassword);
                        if (!created.Succeeded)
                        {
                            logger.LogError("Could not create admin: {Errors}", string.Join("; ", created.Errors.Select(e => e.Description)));
                            return;
                        }
                    }
                    if (!await userManager.IsInRoleAsync(admin, Roles.Admin))
                        await userManager.AddToRoleAsync(admin, Roles.Admin);
                }
                else
                {
                    logger.LogWarning("Seed:AdminEmail / Seed:AdminPassword are not set, so no admin account was created.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database seeding failed");
            }
        }
    }
}
