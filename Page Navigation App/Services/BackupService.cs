using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Page_Navigation_App.Services
{
    public class BackupService
    {
        private readonly AppDbContext _context;
        private readonly string _backupPath;
        private const int MAX_BACKUPS = 30; // Keep last 30 days of backups

        public BackupService(AppDbContext context, string backupPath)
        {
            _context = context;
            _backupPath = backupPath;
            
            if (!Directory.Exists(_backupPath))
            {
                Directory.CreateDirectory(_backupPath);
            }
        }

        public async Task<bool> CreateBackup(string comment = null)
        {
            try
            {
                // Get database file path
                var dbPath = _context.Database.GetDbConnection().DataSource;

                // Create backup filename with timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFile = Path.Combine(_backupPath, $"backup_{timestamp}.db");

                // Ensure database connection is closed
                await _context.Database.CloseConnectionAsync();

                // Copy database file
                File.Copy(dbPath, backupFile);

                // Create metadata file
                var metadataFile = Path.ChangeExtension(backupFile, ".meta");
                await File.WriteAllTextAsync(metadataFile, 
                    $"Timestamp: {DateTime.Now}\n" +
                    $"Comment: {comment}\n" +
                    $"Size: {new FileInfo(backupFile).Length} bytes");

                // Clean up old backups
                await CleanupOldBackups();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                // Ensure database connection is reopened
                await _context.Database.OpenConnectionAsync();
            }
        }

        public async Task<bool> RestoreBackup(string backupFile)
        {
            try
            {
                if (!File.Exists(backupFile))
                    return false;

                var dbPath = _context.Database.GetDbConnection().DataSource;

                // Close all connections
                await _context.Database.CloseConnectionAsync();

                // Create a backup of current database before restore
                var currentBackup = Path.Combine(_backupPath, 
                    $"pre_restore_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                File.Copy(dbPath, currentBackup);

                // Restore the backup
                File.Copy(backupFile, dbPath, true);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                // Reopen connection
                await _context.Database.OpenConnectionAsync();
            }
        }

        public async Task<List<BackupInfo>> ListBackups()
        {
            var backups = new List<BackupInfo>();
            var files = Directory.GetFiles(_backupPath, "backup_*.db");

            foreach (var file in files)
            {
                var metaFile = Path.ChangeExtension(file, ".meta");
                var metadata = File.Exists(metaFile) 
                    ? await File.ReadAllTextAsync(metaFile) 
                    : string.Empty;

                backups.Add(new BackupInfo
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file),
                    CreatedAt = File.GetCreationTime(file),
                    Size = new FileInfo(file).Length,
                    Metadata = metadata
                });
            }

            return backups.OrderByDescending(b => b.CreatedAt).ToList();
        }

        private async Task CleanupOldBackups()
        {
            var backups = await ListBackups();
            
            // Keep only the most recent MAX_BACKUPS backups
            var toDelete = backups.Skip(MAX_BACKUPS).ToList();
            
            foreach (var backup in toDelete)
            {
                try
                {
                    File.Delete(backup.FilePath);
                    var metaFile = Path.ChangeExtension(backup.FilePath, ".meta");
                    if (File.Exists(metaFile))
                        File.Delete(metaFile);
                }
                catch
                {
                    // Log error but continue with other deletions
                    continue;
                }
            }
        }

        public async Task<bool> VerifyBackup(string backupFile)
        {
            try
            {
                // Create a temporary directory for verification
                var tempDir = Path.Combine(_backupPath, "temp_verify");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // Copy backup to temp location
                    var tempDb = Path.Combine(tempDir, "verify.db");
                    File.Copy(backupFile, tempDb);

                    // Try to open the database and perform a simple query
                    var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                    optionsBuilder.UseSqlite($"Data Source={tempDb}");

                    using (var verifyContext = new AppDbContext(optionsBuilder.Options))
                    {
                        // Try to access some data
                        await verifyContext.Customers.FirstOrDefaultAsync();
                        return true;
                    }
                }
                finally
                {
                    // Cleanup
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public class BackupInfo
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedAt { get; set; }
        public long Size { get; set; }
        public string Metadata { get; set; }
    }
}