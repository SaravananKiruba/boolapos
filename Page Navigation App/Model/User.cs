using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class User
    {
        [Key]
        public int UserID { get; set; }
        
        // Create Id property as an alias to UserID for backward compatibility
        [NotMapped]
        public int Id 
        { 
            get { return UserID; } 
            set { UserID = value; } 
        }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; }
        
        [Required]
        public byte[] PasswordHash { get; set; }
        
        [Required]
        public byte[] PasswordSalt { get; set; }
        
        // Add Password property that services are looking for
        [NotMapped]
        public string Password { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }
        
        // Add FirstName and LastName properties
        [NotMapped]
        public string FirstName
        {
            get { return FullName?.Split(' ').Length > 0 ? FullName?.Split(' ')[0] : string.Empty; }
            set 
            { 
                var parts = FullName?.Split(' ');
                if (parts?.Length > 1)
                {
                    FullName = $"{value} {parts[1]}"; 
                }
                else
                {
                    FullName = value; 
                }
            }
        }
        
        [NotMapped]
        public string LastName
        {
            get { return FullName?.Split(' ').Length > 1 ? FullName?.Split(' ')[1] : string.Empty; }
            set 
            { 
                var parts = FullName?.Split(' ');
                if (parts?.Length > 0)
                {
                    FullName = $"{parts[0]} {value}"; 
                }
                else
                {
                    FullName = value; 
                }
            }
        }
        
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }
        
        [Phone]
        [StringLength(15)]
        public string Phone { get; set; }
        
        [Required]
        public DateTime CreatedDate { get; set; }
        
        public DateTime? LastLoginDate { get; set; }
        
        // Add LastPasswordChangeDate property
        public DateTime? LastPasswordChangeDate { get; set; }
        
        [Required]
        public bool IsActive { get; set; }
        
        // Role reference properties
        public int? RoleId { get; set; }
        
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }
        
        public virtual ICollection<Role> Roles { get; set; } = new HashSet<Role>();
    }

    public class UserRole
    {
        [Key]
        public int UserRoleID { get; set; }
        
        [Required]
        public int UserID { get; set; }
        
        [Required]
        [StringLength(50)]
        public string RoleName { get; set; }
        
        public virtual User User { get; set; }
    }
}