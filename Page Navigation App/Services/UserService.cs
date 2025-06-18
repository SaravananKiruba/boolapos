using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
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

        public UserService(AppDbContext context, SecurityService securityService)
        {
            _context = context;
            _securityService = securityService;
        }        public async Task<IEnumerable<User>> GetUsers(bool includeInactive = false)
        {
            var query = _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsQueryable();
                
            if (!includeInactive)
                query = query.Where(u => u.IsActive);
                
            return await query.ToListAsync();
        }        public async Task<User> GetUserById(int id)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == id);
        }        public async Task<User> GetUserByUsername(string username)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<User> CreateUser(User user, string password)
        {
            if (await GetUserByUsername(user.Username) != null)
                throw new InvalidOperationException("Username already exists");

            using (var hmac = new HMACSHA512())
            {
                user.PasswordSalt = hmac.Key;
                user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
            
            user.CreatedDate = DateTime.Now;
            user.IsActive = true;
            
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }        public async Task<bool> UpdateUser(User user)
        {
            var existingUser = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == user.UserID);
                
            if (existingUser == null) return false;

            existingUser.FullName = user.FullName;
            existingUser.Email = user.Email;
            existingUser.Phone = user.Phone;
            existingUser.IsActive = user.IsActive;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResetPassword(int userId, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            using (var hmac = new HMACSHA512())
            {
                user.PasswordSalt = hmac.Key;
                user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(newPassword));
            }
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignRoles(int userId, IEnumerable<string> roles)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Remove existing roles
            var existingRoles = await _context.Set<UserRole>()
                .Where(ur => ur.UserID == userId)
                .ToListAsync();
                
            _context.Set<UserRole>().RemoveRange(existingRoles);

            // Add new roles
            foreach (var roleName in roles)
            {
                await _context.Set<UserRole>().AddAsync(new UserRole
                {
                    UserID = userId,
                    User = user
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}