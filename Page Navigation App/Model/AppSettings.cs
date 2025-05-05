namespace Page_Navigation_App.Model
{
    /// <summary>
    /// Application settings that can be configured globally
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Folder path where database backups are stored
        /// </summary>
        public string BackupFolder { get; set; } = string.Empty;
        
        /// <summary>
        /// Key used for AES encryption of sensitive data
        /// </summary>
        public string EncryptionKey { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether automatic backups are enabled
        /// </summary>
        public bool EnableAutoBackup { get; set; } = true;
        
        /// <summary>
        /// How often backups should be created (in hours)
        /// </summary>
        public int AutoBackupIntervalHours { get; set; } = 24;
        
        /// <summary>
        /// Number of backup files to keep (0 = unlimited)
        /// </summary>
        public int MaxBackupFiles { get; set; } = 10;
        
        /// <summary>
        /// Whether to compress backup files to save space
        /// </summary>
        public bool CompressBackups { get; set; } = true;
        
        /// <summary>
        /// Default location for inventory items
        /// </summary>
        public string DefaultStockLocation { get; set; } = "Main Store";
        
        /// <summary>
        /// Default GST rate for jewelry items
        /// </summary>
        public decimal DefaultGSTRate { get; set; } = 3.0m;
    }
}