using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Page_Navigation_App.Services
{
    /// <summary>
    /// Service to handle database encryption and secure operations
    /// </summary>
    public class SecurityService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;
        private readonly ConfigurationService _configService;
        private readonly AppSettings _settings;
        private readonly string _encryptionKey;

        public SecurityService(
            AppDbContext context,
            LogService logService,
            ConfigurationService configService,
            IOptions<AppSettings> settings)
        {
            _context = context;
            _logService = logService;
            _configService = configService;
            _settings = settings.Value;
            _encryptionKey = _settings.EncryptionKey;
        }

        public async Task<bool> CheckPermission(string userId, string action)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.UserID.ToString() == userId);

            if (user == null || !user.IsActive)
                return false;

            // Admin role has all permissions
            if (user.Roles.Any(r => r.RoleName == "Admin"))
                return true;

            var permissionMatrix = GetPermissionMatrix();
            foreach (var role in user.Roles)
            {
                if (permissionMatrix.TryGetValue(role.RoleName, out var permissions) &&
                    (permissions.Contains(action) || permissions.Contains("*")))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task LogAction(
            string userId,
            string action,
            string description,
            bool isSuccessful)
        {
            var securityLog = new SecurityLog
            {
                Timestamp = DateTime.Now,
                UserID = userId,
                Action = action,
                Description = description,
                IsSuccessful = isSuccessful,
                IPAddress = GetCurrentIPAddress(),
                UserAgent = GetCurrentUserAgent()
            };

            await _context.SecurityLogs.AddAsync(securityLog);
            await _context.SaveChangesAsync();

            if (!isSuccessful)
            {
                _logService.LogWarning(
                    $"Security event: {action} - {description}",
                    "SecurityService",
                    userId);
            }
        }

        public async Task<IEnumerable<SecurityLog>> GetSecurityLogs(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string userId = null,
            string action = null,
            bool? isSuccessful = null)
        {
            var query = _context.SecurityLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(l => l.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.Timestamp <= endDate.Value);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(l => l.UserID == userId);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(l => l.Action == action);

            if (isSuccessful.HasValue)
                query = query.Where(l => l.IsSuccessful == isSuccessful.Value);

            return await query
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public Dictionary<string, List<string>> GetPermissionMatrix()
        {
            // This could be loaded from configuration or database
            return new Dictionary<string, List<string>>
            {
                {
                    "Admin",
                    new List<string> { "*" } // All permissions
                },
                {
                    "Manager",
                    new List<string>
                    {
                        "ViewReports",
                        "ManageUsers",
                        "ManageProducts",
                        "ManageStock",
                        "ManageOrders",
                        "ManageCustomers",
                        "ManageRepairs",
                        "ViewRates",
                        "UpdateRates",
                        "ProcessPayments",
                        "ManageSuppliers",
                        "ViewAuditLogs"
                    }
                },
                {
                    "Sales",
                    new List<string>
                    {
                        "ViewProducts",
                        "CreateOrders",
                        "ViewCustomers",
                        "CreateCustomers",
                        "ViewRates",
                        "ProcessPayments",
                        "CreateRepairs",
                        "UpdateRepairs"
                    }
                }
            };
        }

        public async Task<Dictionary<string, int>> GetSecurityStats(
            DateTime startDate,
            DateTime endDate)
        {
            var logs = await _context.SecurityLogs
                .Where(l => l.Timestamp >= startDate && 
                           l.Timestamp <= endDate)
                .ToListAsync();

            return new Dictionary<string, int>
            {
                { "TotalEvents", logs.Count },
                { "FailedEvents", logs.Count(l => !l.IsSuccessful) },
                { "UniqueUsers", logs.Select(l => l.UserID).Distinct().Count() },
                { "LoginAttempts", logs.Count(l => l.Action == "LoginAttempt") },
                { "FailedLogins", logs.Count(l => 
                    l.Action == "LoginAttempt" && !l.IsSuccessful) }
            };
        }

        private string GetCurrentIPAddress()
        {
            // Implementation would depend on your web framework
            return "127.0.0.1"; // Placeholder
        }

        private string GetCurrentUserAgent()
        {
            // Implementation would depend on your web framework
            return "Unknown"; // Placeholder
        }

        /// <summary>
        /// Encrypts a string using AES encryption
        /// </summary>
        public string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                byte[] iv = new byte[16];
                byte[] array;

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
                    aes.IV = iv;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                            {
                                streamWriter.Write(plainText);
                            }

                            array = memoryStream.ToArray();
                        }
                    }
                }

                return Convert.ToBase64String(array);
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error encrypting string: {ex.Message}");
                return plainText;
            }
        }

        /// <summary>
        /// Decrypts a string that was encrypted with AES
        /// </summary>
        public string DecryptString(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                byte[] iv = new byte[16];
                byte[] buffer = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
                    aes.IV = iv;
                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream(buffer))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader streamReader = new StreamReader(cryptoStream))
                            {
                                return streamReader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error decrypting string: {ex.Message}");
                return cipherText;
            }
        }

        /// <summary>
        /// Encrypts sensitive customer data
        /// </summary>
        public async Task<bool> EncryptCustomerDataAsync(Customer customer)
        {
            try
            {
                // Encrypt sensitive customer data
                if (!string.IsNullOrEmpty(customer.PhoneNumber))
                    customer.PhoneNumber = EncryptString(customer.PhoneNumber);
                
                if (!string.IsNullOrEmpty(customer.Email))
                    customer.Email = EncryptString(customer.Email);
                
                if (!string.IsNullOrEmpty(customer.WhatsAppNumber))
                    customer.WhatsAppNumber = EncryptString(customer.WhatsAppNumber);
                
                _logService.LogInformation($"Encrypted sensitive data for customer {customer.CustomerID}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error encrypting customer data: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Decrypts sensitive customer data for display
        /// </summary>
        public async Task<Customer> DecryptCustomerDataAsync(Customer customer)
        {
            try
            {
                // Create a new instance to avoid modifying the original entity
                Customer decryptedCustomer = new Customer
                {
                    CustomerID = customer.CustomerID,
                    CustomerName = customer.CustomerName,
                    Address = customer.Address,
                    City = customer.City,
                    GSTNumber = customer.GSTNumber,
                    // Decrypt sensitive fields
                    PhoneNumber = DecryptString(customer.PhoneNumber),
                    Email = DecryptString(customer.Email),
                    WhatsAppNumber = DecryptString(customer.WhatsAppNumber),
                    // Copy remaining fields
                    DateOfBirth = customer.DateOfBirth,
                    DateOfAnniversary = customer.DateOfAnniversary,
                    RegistrationDate = customer.RegistrationDate,
                    LoyaltyPoints = customer.LoyaltyPoints,
                    IsActive = customer.IsActive,
                    CreditLimit = customer.CreditLimit,
                    CustomerType = customer.CustomerType,
                    PreferredDesigns = customer.PreferredDesigns,
                    PreferredMetalType = customer.PreferredMetalType,
                    RingSize = customer.RingSize,
                    BangleSize = customer.BangleSize,
                    ChainLength = customer.ChainLength,
                    TotalPurchases = customer.TotalPurchases,
                    OutstandingAmount = customer.OutstandingAmount,
                    IsGoldSchemeEnrolled = customer.IsGoldSchemeEnrolled,
                    LastPurchaseDate = customer.LastPurchaseDate,
                    FamilyDetails = customer.FamilyDetails
                };
                
                return decryptedCustomer;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error decrypting customer data: {ex.Message}");
                return customer; // Return original if decryption fails
            }
        }

        /// <summary>
        /// Creates a secure password hash
        /// </summary>
        public (byte[] Hash, byte[] Salt) CreatePasswordHash(string password)
        {
            using (var hmac = new HMACSHA512())
            {
                var salt = hmac.Key;
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                
                return (hash, salt);
            }
        }

        /// <summary>
        /// Verifies a password against a stored hash
        /// </summary>
        public bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            using (var hmac = new HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != storedHash[i])
                        return false;
                }
                
                return true;
            }
        }
    }
}