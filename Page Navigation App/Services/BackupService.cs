using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Text;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    /// <summary>
    /// Service to handle database backups and restores with encryption
    /// </summary>
    public class BackupService
    {
        private readonly AppSettings _settings;
        private readonly LogService _logService;
        private readonly SecurityService _securityService;
        private readonly string _databasePath;
        private readonly string _backupFolder;

        public BackupService(
            IOptions<AppSettings> settings,
            LogService logService,
            SecurityService securityService)
        {
            _settings = settings.Value;
            _logService = logService;
            _securityService = securityService;
            _databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StockInventory.db");
            _backupFolder = _settings.BackupFolder;
            
            // Ensure backup folder exists
            if (!Directory.Exists(_backupFolder))
            {
                Directory.CreateDirectory(_backupFolder);
            }
        }

        /// <summary>
        /// Create an encrypted backup of the database
        /// </summary>
        public async Task<string> CreateBackupAsync(string description = "")
        {
            try
            {
                if (!File.Exists(_databasePath))
                {
                    await _logService.LogErrorAsync("Database file not found for backup");
                    return null;
                }

                // Create backup filename with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupName = $"backup_{timestamp}.jsmsb";
                string backupPath = Path.Combine(_backupFolder, backupName);
                string metadataPath = Path.Combine(_backupFolder, $"backup_{timestamp}.meta");

                // Create backup metadata
                string metadata = $"Timestamp: {DateTime.Now}\nDescription: {description}\nVersion: 1.0\n";
                File.WriteAllText(metadataPath, _securityService.EncryptString(metadata));

                // Create backup with encryption
                using (FileStream originalFileStream = new FileStream(_databasePath, FileMode.Open, FileAccess.Read))
                using (FileStream backupFileStream = new FileStream(backupPath, FileMode.Create))
                using (var aes = Aes.Create())
                using (CryptoStream cryptoStream = new CryptoStream(
                    backupFileStream,
                    aes.CreateEncryptor(
                        Encoding.UTF8.GetBytes(_settings.EncryptionKey.PadRight(32).Substring(0, 32)),
                        new byte[16]),
                    CryptoStreamMode.Write))
                {
                    await originalFileStream.CopyToAsync(cryptoStream);
                }

                await _logService.LogInformationAsync($"Database backup created: {backupName}");
                return backupPath;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error creating backup: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Restore the database from a backup file
        /// </summary>
        public async Task<bool> RestoreFromBackupAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    await _logService.LogErrorAsync($"Backup file not found: {backupPath}");
                    return false;
                }

                // Create a temporary path for the restored database
                string tempRestorePath = Path.Combine(
                    Path.GetDirectoryName(_databasePath),
                    $"restore_{DateTime.Now:yyyyMMdd_HHmmss}.db");

                // Decrypt and restore the database
                using (FileStream backupFileStream = new FileStream(backupPath, FileMode.Open, FileAccess.Read))
                using (var aes = Aes.Create())
                using (CryptoStream cryptoStream = new CryptoStream(
                    backupFileStream,
                    aes.CreateDecryptor(
                        Encoding.UTF8.GetBytes(_settings.EncryptionKey.PadRight(32).Substring(0, 32)),
                        new byte[16]),
                    CryptoStreamMode.Read))
                using (FileStream restoredFileStream = new FileStream(tempRestorePath, FileMode.Create))
                {
                    await cryptoStream.CopyToAsync(restoredFileStream);
                }

                // Create a backup of the current database before replacing it
                string currentBackupPath = Path.Combine(
                    _backupFolder,
                    $"prerestore_{DateTime.Now:yyyyMMdd_HHmmss}.jsmsb");
                File.Copy(_databasePath, currentBackupPath, true);

                // Replace the current database with the restored one
                File.Copy(tempRestorePath, _databasePath, true);
                File.Delete(tempRestorePath);

                await _logService.LogInformationAsync($"Database restored from backup: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error restoring from backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get a list of available backups
        /// </summary>
        public async Task<List<BackupInfo>> GetBackupListAsync()
        {
            try
            {
                if (!Directory.Exists(_backupFolder))
                {
                    return new List<BackupInfo>();
                }

                var backupFiles = Directory.GetFiles(_backupFolder, "*.jsmsb");
                var backupList = new List<BackupInfo>();

                foreach (var backupFile in backupFiles)
                {
                    string fileName = Path.GetFileName(backupFile);
                    string metadataPath = Path.Combine(
                        _backupFolder,
                        Path.GetFileNameWithoutExtension(backupFile) + ".meta");
                    
                    string description = "No description";
                    if (File.Exists(metadataPath))
                    {
                        string encryptedMetadata = File.ReadAllText(metadataPath);
                        string metadata = _securityService.DecryptString(encryptedMetadata);
                        
                        // Extract description from metadata
                        var descriptionLine = metadata.Split('\n')
                            .FirstOrDefault(line => line.StartsWith("Description:"));
                        
                        if (descriptionLine != null)
                        {
                            description = descriptionLine.Substring("Description:".Length).Trim();
                        }
                    }

                    var fileInfo = new FileInfo(backupFile);
                    backupList.Add(new BackupInfo
                    {
                        FileName = fileName,
                        Description = description,
                        Size = fileInfo.Length,
                        CreatedDate = fileInfo.CreationTime
                    });
                }

                return backupList.OrderByDescending(b => b.CreatedDate).ToList();
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting backup list: {ex.Message}");
                return new List<BackupInfo>();
            }
        }

        /// <summary>
        /// Schedule automatic backups
        /// </summary>
        public async Task ScheduleAutomaticBackupsAsync()
        {
            try
            {
                if (!_settings.EnableAutoBackup)
                {
                    await _logService.LogInformationAsync("Automatic backups are disabled in settings");
                    return;
                }

                // Get the last backup time
                var backups = await GetBackupListAsync();
                var lastBackup = backups.FirstOrDefault();
                
                if (lastBackup != null)
                {
                    // Check if it's time for a new backup based on the interval
                    var hoursSinceLastBackup = (DateTime.Now - lastBackup.CreatedDate).TotalHours;
                    
                    if (hoursSinceLastBackup < _settings.AutoBackupIntervalHours)
                    {
                        // Not time for a backup yet
                        return;
                    }
                }

                // Create a new automatic backup
                await CreateBackupAsync("Automatic scheduled backup");
                await _logService.LogInformationAsync("Automatic backup created successfully");
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error scheduling automatic backup: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Information about a database backup
    /// </summary>
    public class BackupInfo
    {
        public string FileName { get; set; }
        public string Description { get; set; }
        public long Size { get; set; }
        public DateTime CreatedDate { get; set; }
        
        public string SizeFormatted => FormatSize(Size);
        
        private string FormatSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            
            return $"{number:n1} {suffixes[counter]}";
        }
    }
}