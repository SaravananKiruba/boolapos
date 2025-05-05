using System;
using System.ComponentModel.DataAnnotations;

namespace Page_Navigation_App.Model
{
    public class Setting
    {
        [Key]
        public int ID { get; set; }
        
        // Properties referenced in ConfigurationService
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        
        // Add UpdatedDate property that was referenced but missing
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
        
        // Business Settings
        public bool LowStockAlerts { get; set; }
        public bool PaymentReminders { get; set; }
        public int LowStockThreshold { get; set; }

        // Backup Settings
        public string BackupPath { get; set; }

        // Other Settings
        public bool AutoBackup { get; set; }
        public int BackupFrequencyDays { get; set; }
        public bool DarkMode { get; set; }
        public string Language { get; set; }
        public string Currency { get; set; }
    }
}