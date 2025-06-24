using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Microsoft.EntityFrameworkCore;

namespace Page_Navigation_App.Utilities
{
    public static class TestDataSeeder
    {
        public static async Task SeedTestUsersAsync(AppDbContext context, AuthenticationService authService)
        {
            // Check if test users already exist
            if (await context.Users.AnyAsync(u => u.Username == "manager" || u.Username == "cashier" || u.Username == "inventory"))
            {
                // Users already seeded
                return;
            }
            
            // Ensure we have roles first
            await SeedRolesAsync(context);
            
            // Create test users with different roles
            // 1. Manager role
            var managerUser = new User
            {
                Username = "manager",
                FullName = "Test Manager",
                Email = "manager@test.com",
                Phone = "1234567890",
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            await authService.RegisterAsync(managerUser, "Manager@123");
            
            // 2. Cashier role
            var cashierUser = new User
            {
                Username = "cashier",
                FullName = "Test Cashier",
                Email = "cashier@test.com",
                Phone = "2345678901",
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            await authService.RegisterAsync(cashierUser, "Cashier@123");
            
            // 3. Inventory role
            var inventoryUser = new User
            {
                Username = "inventory",
                FullName = "Test Inventory",
                Email = "inventory@test.com",
                Phone = "3456789012",
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            await authService.RegisterAsync(inventoryUser, "Inventory@123");
            
            // Assign roles to users
            await AssignRolesToTestUsersAsync(context);
        }
        
        private static async Task SeedRolesAsync(AppDbContext context)
        {
            // Check if roles already exist
            if (!await context.Roles.AnyAsync())
            {
                // Create basic roles
                var roles = new List<Role>
                {
                    new Role { RoleName = "Admin", Description = "Full system access" },
                    new Role { RoleName = "Manager", Description = "Management access to most features" },
                    new Role { RoleName = "Cashier", Description = "Access to sales and customer operations" },
                    new Role { RoleName = "Inventory", Description = "Access to product and inventory management" }
                };
                
                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }
        }
        
        private static async Task AssignRolesToTestUsersAsync(AppDbContext context)
        {
            // Get users and roles
            var managerUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "manager");
            var cashierUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "cashier");
            var inventoryUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "inventory");
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            var managerRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Manager");
            var cashierRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Cashier");
            var inventoryRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Inventory");
            
            // Make sure we have the admin role assigned to admin user
            if (adminUser != null && adminRole != null)
            {
                if (!await context.UserRoles.AnyAsync(ur => ur.UserID == adminUser.UserID && ur.RoleID == adminRole.RoleID))
                {
                    context.UserRoles.Add(new UserRole
                    {
                        UserID = adminUser.UserID,
                        RoleID = adminRole.RoleID
                    });
                }
            }
            
            // Assign roles
            if (managerUser != null && managerRole != null)
            {
                context.UserRoles.Add(new UserRole
                {
                    UserID = managerUser.UserID,
                    RoleID = managerRole.RoleID
                });
            }
            
            if (cashierUser != null && cashierRole != null)
            {
                context.UserRoles.Add(new UserRole
                {
                    UserID = cashierUser.UserID,
                    RoleID = cashierRole.RoleID
                });
            }
            
            if (inventoryUser != null && inventoryRole != null)
            {
                context.UserRoles.Add(new UserRole
                {
                    UserID = inventoryUser.UserID,
                    RoleID = inventoryRole.RoleID
                });
            }
            
            await context.SaveChangesAsync();
        }
    }
}
