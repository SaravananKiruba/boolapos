using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;

namespace Page_Navigation_App.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;
        private readonly SecurityService _securityService;

        public UserService(
            AppDbContext context,
            SecurityService securityService)
        {
            _context = context;
            _securityService = securityService;
        }

        public async Task<User> CreateUser(User user, string password)
        {
            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                return null;

            // Hash password
            (user.PasswordHash, user.PasswordSalt) = HashPassword(password);
            
            user.CreatedDate = DateTime.Now;
            user.IsActive = true;
            user.LastLoginDate = null;

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            await _securityService.LogAction(
                user.Username,
                "UserCreated",
                $"New user account created: {user.Username}",
                true);

            return user;
        }

        public async Task<User> AuthenticateUser(string username, string password)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || !user.IsActive)
                return null;

            if (!VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            {
                await _securityService.LogAction(
                    username,
                    "LoginFailed",
                    "Invalid password attempt",
                    false);
                return null;
            }

            user.LastLoginDate = DateTime.Now;
            await _context.SaveChangesAsync();

            await _securityService.LogAction(
                user.Username,
                "LoginSuccess",
                "User logged in successfully",
                true);

            return user;
        }

        private (byte[] hash, byte[] salt) HashPassword(string password)
        {
            using var hmac = new HMACSHA512();
            var salt = hmac.Key;
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return (hash, salt);
        }

        private bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
        {
            using var hmac = new HMACSHA512(storedSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(storedHash);
        }

        public async Task<bool> ChangePassword(
            int userId,
            string currentPassword,
            string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            if (!VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
            {
                await _securityService.LogAction(
                    user.Username,
                    "PasswordChangeFailed",
                    "Invalid current password",
                    false);
                return false;
            }

            (user.PasswordHash, user.PasswordSalt) = HashPassword(newPassword);
            await _context.SaveChangesAsync();

            await _securityService.LogAction(
                user.Username,
                "PasswordChanged",
                "Password changed successfully",
                true);

            return true;
        }

        public async Task<bool> ResetPassword(int userId, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            (user.PasswordHash, user.PasswordSalt) = HashPassword(newPassword);
            await _context.SaveChangesAsync();

            await _securityService.LogAction(
                user.Username,
                "PasswordReset",
                "Password was reset by admin",
                true);

            return true;
        }

        public async Task<bool> AssignRoles(int userId, List<string> roles)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
                return false;

            user.Roles.Clear();
            foreach (var role in roles)
            {
                user.Roles.Add(new UserRole { RoleName = role });
            }

            await _context.SaveChangesAsync();

            await _securityService.LogAction(
                user.Username,
                "RolesUpdated",
                $"User roles updated to: {string.Join(", ", roles)}",
                true);

            return true;
        }

        public async Task<IEnumerable<User>> GetUsers(bool includeInactive = false)
        {
            var query = _context.Users.Include(u => u.Roles).AsQueryable();

            if (!includeInactive)
                query = query.Where(u => u.IsActive);

            return await query.ToListAsync();
        }

        public async Task<bool> DeactivateUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.IsActive = false;
            await _context.SaveChangesAsync();

            await _securityService.LogAction(
                user.Username,
                "UserDeactivated",
                "User account was deactivated",
                true);

            return true;
        }

        public async Task<bool> ActivateUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.IsActive = true;
            await _context.SaveChangesAsync();

            await _securityService.LogAction(
                user.Username,
                "UserActivated",
                "User account was activated",
                true);

            return true;
        }

        public async Task<Dictionary<string, int>> GetUserStats()
        {
            var users = await _context.Users
                .Include(u => u.Roles)
                .ToListAsync();

            return new Dictionary<string, int>
            {
                { "TotalUsers", users.Count },
                { "ActiveUsers", users.Count(u => u.IsActive) },
                { "InactiveUsers", users.Count(u => !u.IsActive) },
                { "AdminUsers", users.Count(u => u.Roles.Any(r => r.RoleName == "Admin")) },
                { "ManagerUsers", users.Count(u => u.Roles.Any(r => r.RoleName == "Manager")) },
                { "SalesUsers", users.Count(u => u.Roles.Any(r => r.RoleName == "Sales")) }
            };
        }
    }

    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool IsActive { get; set; }
        public virtual ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
    }

    public class UserRole
    {
        public int UserRoleID { get; set; }
        public int UserID { get; set; }
        public string RoleName { get; set; }
        public virtual User User { get; set; }
    }
}