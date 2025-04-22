using System;

namespace Page_Navigation_App.Model
{
    public class RepairJob
    {
        public int Id { get; set; }
        public string ItemDetails { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string WorkType { get; set; }
        public decimal EstimatedAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string Status { get; set; } // Pending, In Process, Delivered
        public string CustomerId { get; set; } // Foreign key to Customer
    }
}