using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{    public class User
    {
        [Key]
        public int UserID { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; }
        
        [Required]
        public byte[] PasswordHash { get; set; }
        
        [Required]
        public byte[] PasswordSalt { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }
        
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }
        
        [Phone]
        [StringLength(15)]
        public string Phone { get; set; }
        
        [Required]
        public DateTime CreatedDate { get; set; }
        
        public DateTime? LastLoginDate { get; set; }
        
        [Required]
        public bool IsActive { get; set; }
        
        [NotMapped]
        public virtual ICollection<Role> Roles { get; set; } = new HashSet<Role>();
        
        public virtual ICollection<UserRole> UserRoles { get; set; } = new HashSet<UserRole>();
    }    public class UserRole
    {
        [Key]
        public int UserRoleID { get; set; }
        
        [Required]
        public int UserID { get; set; }
        
        [Required]
        public int RoleID { get; set; }
        
        public virtual User User { get; set; }
        
        [ForeignKey("RoleID")]
        public virtual Role Role { get; set; }
    }
}