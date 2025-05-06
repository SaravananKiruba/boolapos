using System;
using System.Collections.Generic;

namespace Page_Navigation_App.Model
{
    public class Role
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        
        // Add Name property as an alias to RoleName for backward compatibility
        public string Name
        {
            get { return RoleName; }
            set { RoleName = value; }
        }
        
        // Add missing properties referenced in code
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}