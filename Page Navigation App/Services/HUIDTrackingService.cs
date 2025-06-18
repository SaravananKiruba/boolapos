using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    /// <summary>
    /// Service to manage Hallmarking Unique ID (HUID) for jewelry items as per BIS requirements
    /// </summary>
    public class HUIDTrackingService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;
        
        // Regular expression for validating HUID format (6-digit alphanumeric)
        private readonly Regex _huidRegex = new Regex(@"^[A-Z0-9]{6}$", RegexOptions.Compiled);

        public HUIDTrackingService(
            AppDbContext context,
            LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        /// <summary>
        /// Validate if a HUID is in the correct format
        /// </summary>
        public bool IsValidHUID(string huid)
        {
            if (string.IsNullOrWhiteSpace(huid))
                return false;
                
            return _huidRegex.IsMatch(huid);
        }

        /// <summary>
        /// Register a new HUID for a product
        /// </summary>
        public async Task<bool> RegisterHUIDAsync(int productId, string huid, string ahc, string jewelType)
        {
            try
            {
                if (!IsValidHUID(huid))
                {
                    await _logService.LogErrorAsync($"Invalid HUID format: {huid}");
                    return false;
                }
                
                // Check if HUID already exists
                bool exists = await _context.Products
                    .AnyAsync(p => p.HUID == huid && p.ProductID != productId);
                    
                if (exists)
                {
                    await _logService.LogErrorAsync($"HUID {huid} already exists in the system");
                    return false;
                }
                
                // Get the product
                var product = await _context.Products.FindAsync(productId);
                
                if (product == null)
                {
                    await _logService.LogErrorAsync($"Product not found: {productId}");
                    return false;
                }
                
                // Update product with HUID information
                product.HUID = huid;
                product.JewelType = jewelType;
                product.HUIDRegistrationDate = DateOnly.FromDateTime(DateTime.Now);
                
                // Update product
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
                
                // Add to HUID tracking log
                await LogHUIDActivityAsync(huid, productId, "Registration", $"HUID registered for {product.ProductName}");
                
                await _logService.LogInformationAsync($"Registered HUID {huid} for product {productId} ({product.ProductName})");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error registering HUID: {ex.Message}", exception: ex);
                return false;
            }
        }

        /// <summary>
        /// Track HUID activity (sale, exchange, etc.)
        /// </summary>
        public async Task<bool> LogHUIDActivityAsync(string huid, int productId, string activityType, string description)
        {
            try
            {
                var huidLog = new Model.HUIDLog
                {
                    HUID = huid,
                    ProductID = productId,
                    ActivityType = activityType,
                    Description = description,
                    ActivityDate = DateTime.Now
                };
                
                // Add to HUID log
                _context.HUIDLogs.Add(huidLog);
                await _context.SaveChangesAsync();
                
                await _logService.LogInformationAsync($"Logged HUID activity: {activityType} for HUID {huid}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error logging HUID activity: {ex.Message}", exception: ex);
                return false;
            }
        }

        /// <summary>
        /// Get product by HUID
        /// </summary>
        public async Task<Product> GetProductByHUIDAsync(string huid)
        {
            try
            {
                if (!IsValidHUID(huid))
                {
                    await _logService.LogErrorAsync($"Invalid HUID format in search: {huid}");
                    return null;
                }
                
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.HUID == huid);
                
                if (product == null)
                {
                    await _logService.LogWarningAsync($"No product found with HUID: {huid}");
                }
                
                return product;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting product by HUID: {ex.Message}", exception: ex);
                return null;
            }
        }

        /// <summary>
        /// Get HUID activity history
        /// </summary>
        public async Task<List<Model.HUIDLog>> GetHUIDHistoryAsync(string huid)
        {
            try
            {
                if (!IsValidHUID(huid))
                {
                    await _logService.LogErrorAsync($"Invalid HUID format in history request: {huid}");
                    return new List<Model.HUIDLog>();
                }
                
                var history = await _context.HUIDLogs
                    .Where(h => h.HUID == huid)
                    .OrderByDescending(h => h.ActivityDate)
                    .ToListAsync();
                
                return history;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting HUID history: {ex.Message}", exception: ex);
                return new List<Model.HUIDLog>();
            }
        }

        /// <summary>
        /// Transfer HUID to a new customer (for tracking ownership)
        /// </summary>
        public async Task<bool> TransferHUIDOwnershipAsync(string huid, int fromCustomerId, int toCustomerId, string transferReason)
        {
            try
            {
                if (!IsValidHUID(huid))
                {
                    await _logService.LogErrorAsync($"Invalid HUID format in transfer: {huid}");
                    return false;
                }
                
                // Get the product
                var product = await GetProductByHUIDAsync(huid);
                
                if (product == null)
                {
                    await _logService.LogErrorAsync($"Cannot transfer ownership: No product found with HUID: {huid}");
                    return false;
                }
                
                // Get customer information for logging
                var fromCustomer = await _context.Customers.FindAsync(fromCustomerId);
                var toCustomer = await _context.Customers.FindAsync(toCustomerId);
                
                if (fromCustomer == null || toCustomer == null)
                {
                    await _logService.LogErrorAsync($"Cannot transfer ownership: Invalid customer IDs");
                    return false;
                }
                
                // Log the transfer
                await LogHUIDActivityAsync(
                    huid, 
                    product.ProductID, 
                    "Ownership Transfer", 
                    $"Transferred from {fromCustomer.FirstName} {fromCustomer.LastName} to {toCustomer.FirstName} {toCustomer.LastName}. Reason: {transferReason}"
                );
                
                await _logService.LogInformationAsync($"Transferred HUID {huid} ownership from customer {fromCustomerId} to {toCustomerId}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error transferring HUID ownership: {ex.Message}", exception: ex);
                return false;
            }
        }

        /// <summary>
        /// Verify HUID authenticity with BIS database (mock implementation)
        /// </summary>
        public async Task<HUIDVerificationResult> VerifyHUIDWithBISAsync(string huid)
        {
            try
            {
                if (!IsValidHUID(huid))
                {
                    await _logService.LogErrorAsync($"Invalid HUID format in verification: {huid}");
                    return new HUIDVerificationResult
                    {
                        IsValid = false,
                        Message = "Invalid HUID format",
                        HUID = huid
                    };
                }
                
                // This is a mock implementation, in reality this would call an external API
                // or check with the BIS database
                
                // For demo, validate all HUIDs but add a delay to simulate network call
                await Task.Delay(500);
                
                // Check if we have this HUID in our system first
                var product = await GetProductByHUIDAsync(huid);
                
                if (product == null)
                {
                    // Unknown to our system, but could still be valid in BIS
                    return new HUIDVerificationResult
                    {
                        IsValid = true, // Assume valid for demo
                        HUID = huid,
                        Message = "HUID verified with BIS, but not found in local system",
                        VerificationDate = DateTime.Now,
                        JewelType = "Unknown", // Would come from BIS API
                        Purity = "Unknown", // Would come from BIS API
                        AHCCode = "Unknown" // Would come from BIS API
                    };
                }
                
                // For known products, return actual details
                return new HUIDVerificationResult
                {
                    IsValid = true,
                    HUID = huid,
                    Message = "HUID verified successfully",
                    VerificationDate = DateTime.Now,
                    JewelType = product.JewelType,
                    Purity = product.Purity,
                    AHCCode = product.AHCCode,
                    ProductID = product.ProductID,
                    ProductName = product.ProductName
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error verifying HUID with BIS: {ex.Message}", exception: ex);
                return new HUIDVerificationResult
                {
                    IsValid = false,
                    HUID = huid,
                    Message = $"Verification error: {ex.Message}",
                    VerificationDate = DateTime.Now
                };
            }
        }

        /// <summary>
        /// Generate BIS compliance report for auditing
        /// </summary>
        public async Task<HUIDComplianceReport> GenerateHUIDComplianceReportAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                // Get products with HUIDs registered in the date range
                var products = await _context.Products
                    .Where(p => !string.IsNullOrEmpty(p.HUID) && 
                               p.HUIDRegistrationDate >= fromDate && 
                               p.HUIDRegistrationDate <= toDate)
                    .ToListAsync();
                
                // Get HUID activity logs in the date range
                var huidLogs = await _context.HUIDLogs
                    .Where(h => h.ActivityDate >= fromDate && h.ActivityDate <= toDate)
                    .OrderByDescending(h => h.ActivityDate)
                    .ToListAsync();
                
                // Group products by jewelry type
                var jewelryTypeStats = products
                    .GroupBy(p => p.JewelType)
                    .Select(g => new JewelryTypeStats
                    {
                        JewelryType = g.Key,
                        Count = g.Count()
                    })
                    .ToList();
                
                // Group products by AHC
                var ahcStats = products
                    .GroupBy(p => p.AHCCode)
                    .Select(g => new AHCStats
                    {
                        AHCCode = g.Key,
                        Count = g.Count()
                    })
                    .ToList();
                
                // Group activity logs by type
                var activityStats = huidLogs
                    .GroupBy(h => h.ActivityType)
                    .Select(g => new ActivityStats
                    {
                        ActivityType = g.Key,
                        Count = g.Count()
                    })
                    .ToList();
                
                // Generate the report
                var report = new HUIDComplianceReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    GeneratedDate = DateTime.Now,
                    TotalHUIDsRegistered = products.Count,
                    JewelryTypeStatistics = jewelryTypeStats,
                    AHCStatistics = ahcStats,
                    ActivityStatistics = activityStats,
                    RecentActivities = huidLogs.Take(50).ToList() // Recent 50 activities
                };
                
                await _logService.LogInformationAsync($"Generated HUID compliance report from {fromDate:dd-MMM-yyyy} to {toDate:dd-MMM-yyyy}");
                return report;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating HUID compliance report: {ex.Message}", exception: ex);
                return new HUIDComplianceReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    GeneratedDate = DateTime.Now,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    /// <summary>
    /// HUID verification result
    /// </summary>
    public class HUIDVerificationResult
    {
        public bool IsValid { get; set; }
        public string HUID { get; set; }
        public string Message { get; set; }
        public DateTime VerificationDate { get; set; }
        public string JewelType { get; set; }
        public string Purity { get; set; }
        public string AHCCode { get; set; }
        public int? ProductID { get; set; }
        public string ProductName { get; set; }
    }

    /// <summary>
    /// HUID compliance report for a period
    /// </summary>
    public class HUIDComplianceReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string ErrorMessage { get; set; }
        public int TotalHUIDsRegistered { get; set; }
        public List<JewelryTypeStats> JewelryTypeStatistics { get; set; } = new List<JewelryTypeStats>();
        public List<AHCStats> AHCStatistics { get; set; } = new List<AHCStats>();
        public List<ActivityStats> ActivityStatistics { get; set; } = new List<ActivityStats>();
        public List<Model.HUIDLog> RecentActivities { get; set; } = new List<Model.HUIDLog>();
    }

    /// <summary>
    /// Jewelry type statistics
    /// </summary>
    public class JewelryTypeStats
    {
        public string JewelryType { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// AHC statistics
    /// </summary>
    public class AHCStats
    {
        public string AHCCode { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// Activity statistics
    /// </summary>
    public class ActivityStats
    {
        public string ActivityType { get; set; }
        public int Count { get; set; }
    }
}