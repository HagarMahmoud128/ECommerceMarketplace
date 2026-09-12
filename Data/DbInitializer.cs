using ECommerceMarketplace.Models;
using Microsoft.AspNetCore.Identity;

namespace ECommerceMarketplace.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            
            // Seed roles
            string[] roles = { "Admin", "Seller", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Seed starter categories
            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    new Category { Name = "Electronics", Description = "Phones, laptops and gadgets" },
                    new Category { Name = "Clothing", Description = "Men, women and kids clothing" },
                    new Category { Name = "Home & Kitchen", Description = "Furniture and appliances" },
                    new Category { Name = "Books", Description = "Fiction and non-fiction" },
                    new Category { Name = "Sports", Description = "Sports and outdoor equipment" }
                );
                context.SaveChanges();
            }

            // Seed a default administrator account
            const string adminEmail = "admin@marketplace.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@12345");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
