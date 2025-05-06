using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    public class AuthenticationService
    {
        private readonly AppDbContext _dbContext;
        private readonly SecurityService _securityService;
        private readonly LogService _logService;
        private User _currentUser;

        public AuthenticationService(AppDbContext dbContext, SecurityService securityService, LogService logService)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public User CurrentUser => _currentUser;

        public bool IsAuthenticated => _currentUser != null;

        public bool AuthenticateUser(string username, string password)
        {
            try
            {
                // Hash the password for comparison
                string hashedPassword = HashPassword(password);

                // Find user in database
                var user = _dbContext.Users
                    .Include(u => u.Role)
                    .FirstOrDefault(u => u.Username.ToLower() == username.ToLower() && u.IsActive);

                if (user == null)
                {
                    _logService.LogWarning($"Authentication failed: User {username} not found or inactive");
                    return false;
                }

                // Verify password
                bool isValid = string.Equals(user.Password, hashedPassword);

                if (isValid)
                {
                    // Set current user on successful login
                    _currentUser = user;
                    
                    // Log successful login
                    _logService.LogInfo($"User {username} authenticated successfully");
                    
                    // Update last login timestamp
                    user.LastLoginDate = DateTime.Now;
                    _dbContext.SaveChanges();
                    
                    // Create security log entry
                    _securityService.LogLoginActivity(user.Id, true);
                    
                    return true;
                }
                else
                {
                    _logService.LogWarning($"Authentication failed: Invalid password for user {username}");
                    
                    // Create security log entry for failed login
                    if (user != null)
                    {
                        _securityService.LogLoginActivity(user.Id, false);
                    }
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"Authentication error: {ex.Message}");
                return false;
            }
        }

        public void Logout()
        {
            if (_currentUser != null)
            {
                _securityService.LogLogoutActivity(_currentUser.Id);
                _logService.LogInfo($"User {_currentUser.Username} logged out");
                _currentUser = null;
            }
        }

        public async Task SeedDefaultUserAsync()
        {
            try
            {
                // Check if admin user exists
                if (!await _dbContext.Users.AnyAsync(u => u.Username.ToLower() == "admin"))
                {
                    // Check if we have any roles
                    if (!await _dbContext.Roles.AnyAsync())
                    {
                        // Create default admin role
                        var adminRole = new Role
                        {
                            Name = "Administrator",
                            Description = "System Administrator with full access",
                            CreatedDate = DateTime.Now,
                            IsActive = true
                        };

                        _dbContext.Roles.Add(adminRole);
                        await _dbContext.SaveChangesAsync();
                    }

                    var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
                    if (role == null)
                    {
                        role = await _dbContext.Roles.FirstOrDefaultAsync();
                    }

                    // Create default admin user
                    var adminUser = new User
                    {
                        Username = "admin",
                        Password = HashPassword("Admin@123"), // Default password
                        Email = "admin@boolapos.com",
                        FirstName = "System",
                        LastName = "Administrator",
                        RoleId = role.RoleID,
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        LastPasswordChangeDate = DateTime.Now
                    };

                    _dbContext.Users.Add(adminUser);
                    await _dbContext.SaveChangesAsync();

                    _logService.LogInfo("Default administrator account created");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error seeding default user: {ex.Message}");
                throw;
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                
                return builder.ToString();
            }
        }
    }
}