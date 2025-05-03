using System;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    public class BackupService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BackupService> _logger;
        private readonly string _backupFolder;
        private readonly string _encryptionKey;

        public BackupService(
            AppDbContext context,
            ILogger<BackupService> logger,
            IOptions<AppSettings> appSettings)
        {
            _context = context;
            _logger = logger;
            _backupFolder = appSettings.Value.BackupFolder ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            _encryptionKey = appSettings.Value.EncryptionKey ?? "JSMS_DefaultSecureEncryptionKey_2025";
            
            // Ensure backup directory exists
            if (!Directory.Exists(_backupFolder))
            {
                Directory.CreateDirectory(_backupFolder);
            }
        }

        /// <summary>
        /// Creates an encrypted backup of the database
        /// </summary>
        public async Task<string> CreateBackupAsync(string comment = "")
        {
            try
            {
                _logger.LogInformation("Starting database backup process");
                
                // Get database connection string
                var connection = _context.Database.GetDbConnection();
                string dbPath = connection.DataSource;
                
                if (string.IsNullOrEmpty(dbPath))
                {
                    throw new Exception("Cannot determine database file path");
                }

                // Generate backup filename with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFilename = $"JSMS_Backup_{timestamp}.db.bak";
                string backupPath = Path.Combine(_backupFolder, backupFilename);
                string encryptedBackupPath = $"{backupPath}.aes";
                
                // First create a copy of the SQLite database file
                using (var source = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var destination = new FileStream(backupPath, FileMode.Create, FileAccess.Write))
                {
                    await source.CopyToAsync(destination);
                }
                
                // Compress the backup
                string compressedBackupPath = $"{backupPath}.zip";
                using (var zipFile = ZipFile.Open(compressedBackupPath, ZipArchiveMode.Create))
                {
                    zipFile.CreateEntryFromFile(backupPath, Path.GetFileName(backupPath));
                }
                
                // Delete the uncompressed backup
                File.Delete(backupPath);
                
                // Encrypt the compressed backup
                EncryptFile(compressedBackupPath, encryptedBackupPath, _encryptionKey);
                
                // Delete the compressed backup
                File.Delete(compressedBackupPath);
                
                // Log the backup in the database
                await LogBackupAsync(encryptedBackupPath, comment);
                
                _logger.LogInformation($"Database backup completed successfully: {encryptedBackupPath}");
                return encryptedBackupPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database backup process");
                throw;
            }
        }
        
        /// <summary>
        /// Restores a database from an encrypted backup file
        /// </summary>
        public async Task<bool> RestoreBackupAsync(string backupFilePath)
        {
            try
            {
                _logger.LogInformation($"Starting database restore from: {backupFilePath}");
                
                if (!File.Exists(backupFilePath))
                {
                    throw new FileNotFoundException("Backup file not found", backupFilePath);
                }
                
                // Get database connection string
                var connection = _context.Database.GetDbConnection();
                string dbPath = connection.DataSource;
                
                // Create a temporary location for the decrypted and uncompressed backup
                string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempFolder);
                
                string decryptedBackupPath = Path.Combine(tempFolder, "backup.db.zip");
                string uncompressedBackupPath = Path.Combine(tempFolder, "backup.db");
                
                try
                {
                    // Decrypt the backup
                    DecryptFile(backupFilePath, decryptedBackupPath, _encryptionKey);
                    
                    // Decompress the backup
                    using (var zipFile = ZipFile.OpenRead(decryptedBackupPath))
                    {
                        var entry = zipFile.Entries[0];
                        entry.ExtractToFile(uncompressedBackupPath, true);
                    }
                    
                    // Close the current database connection
                    await _context.Database.CloseConnectionAsync();
                    
                    // Copy the backup file to the database location
                    using (var source = new FileStream(uncompressedBackupPath, FileMode.Open, FileAccess.Read))
                    using (var destination = new FileStream(dbPath, FileMode.Create, FileAccess.Write))
                    {
                        await source.CopyToAsync(destination);
                    }
                    
                    _logger.LogInformation("Database restore completed successfully");
                    return true;
                }
                finally
                {
                    // Clean up temp files
                    if (Directory.Exists(tempFolder))
                    {
                        Directory.Delete(tempFolder, true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database restore process");
                throw;
            }
        }
        
        /// <summary>
        /// Encrypts a file using AES encryption
        /// </summary>
        private void EncryptFile(string inputFile, string outputFile, string key)
        {
            byte[] keyBytes = GetKeyBytes(key);
            byte[] iv = new byte[16]; // AES block size is 16 bytes
            
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                
                using (var inputFileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                using (var outputFileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                {
                    // Write IV to the beginning of the output file
                    outputFileStream.Write(iv, 0, iv.Length);
                    
                    using (var cryptoStream = new CryptoStream(
                        outputFileStream, 
                        aes.CreateEncryptor(), 
                        CryptoStreamMode.Write))
                    {
                        inputFileStream.CopyTo(cryptoStream);
                    }
                }
            }
        }
        
        /// <summary>
        /// Decrypts a file using AES encryption
        /// </summary>
        private void DecryptFile(string inputFile, string outputFile, string key)
        {
            byte[] keyBytes = GetKeyBytes(key);
            
            using (var inputFileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            {
                // Read IV from the beginning of the encrypted file
                byte[] iv = new byte[16]; // AES block size
                inputFileStream.Read(iv, 0, iv.Length);
                
                using (var aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.Key = keyBytes;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    
                    using (var outputFileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                    using (var cryptoStream = new CryptoStream(
                        inputFileStream, 
                        aes.CreateDecryptor(), 
                        CryptoStreamMode.Read))
                    {
                        cryptoStream.CopyTo(outputFileStream);
                    }
                }
            }
        }
        
        /// <summary>
        /// Derives a 256-bit key from the provided string using PBKDF2
        /// </summary>
        private byte[] GetKeyBytes(string key)
        {
            // Use a fixed salt for deterministic key derivation
            byte[] salt = new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };
            
            using (var derivation = new Rfc2898DeriveBytes(key, salt, 10000, HashAlgorithmName.SHA256))
            {
                return derivation.GetBytes(32); // 256 bits = 32 bytes
            }
        }
        
        /// <summary>
        /// Logs the backup operation in the database
        /// </summary>
        private async Task LogBackupAsync(string backupPath, string comment)
        {
            var backupLog = new AuditLog
            {
                Action = "Database Backup",
                EntityName = "Database",
                Details = $"Backup created: {Path.GetFileName(backupPath)}",
                Timestamp = DateTime.Now,
                UserID = "System",
                IPAddress = "127.0.0.1",
                ModifiedAt = DateTime.Now,
                ModifiedBy = "System"
            };
            
            _context.AuditLogs.Add(backupLog);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Configuration settings for the application
    /// </summary>
    public class AppSettings
    {
        public string BackupFolder { get; set; }
        public string EncryptionKey { get; set; }
        public int AutoBackupIntervalHours { get; set; } = 24;
        public bool EnableAutoBackup { get; set; } = true;
    }
}