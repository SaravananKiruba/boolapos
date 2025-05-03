using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System.Collections.Generic;
using System.Linq;

namespace Page_Navigation_App.Services
{
    public class AuthenticationService
    {
        private readonly AppDbContext _context;

        // Demo user list - in a real application, this would come from a database
        private readonly List<User> _users = new List<User>
        {
            new User 
            { 
                UserID = 1, 
                Username = "admin", 
                PasswordHash = Encoding.UTF8.GetBytes("password"), // Converted to byte[]
                PasswordSalt = new byte[128], // Added PasswordSalt
                FullName = "System Administrator",
                Email = "admin@example.com",
                IsActive = true,
                CreatedDate = DateTime.Now
                // Removed RoleId as it doesn't exist in the model
            },
            new User 
            { 
                UserID = 2, 
                Username = "cashier", 
                PasswordHash = Encoding.UTF8.GetBytes("cashier123"), // Converted to byte[]
                PasswordSalt = new byte[128], // Added PasswordSalt
                FullName = "Cashier User",
                Email = "cashier@example.com",
                IsActive = true,
                CreatedDate = DateTime.Now
                // Removed RoleId as it doesn't exist in the model
            }
        };

        public AuthenticationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user == null)
                return null;

            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return null;

            // Update last login date
            user.LastLoginDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<User> RegisterAsync(User user, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                throw new Exception("Username already exists");

            if (string.IsNullOrEmpty(password))
                throw new Exception("Password is required");

            CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            user.CreatedDate = DateTime.Now;
            user.IsActive = true;

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return user;
        }

        private bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            if (storedHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected)");
            if (storedSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected)");

            using (var hmac = new HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != storedHash[i]) return false;
                }
            }

            return true;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        // Seed an admin user if none exists
        public async Task SeedDefaultUserAsync()
        {
            if (!await _context.Users.AnyAsync())
            {
                var adminUser = new User
                {
                    Username = "admin",
                    FullName = "Administrator",
                    Email = "admin@boolasystem.com",
                    IsActive = true
                };

                await RegisterAsync(adminUser, "Admin@123");
            }
        }
    }
}