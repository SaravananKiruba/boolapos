using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class Finance
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string OrderReference { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal InterestRate { get; set; }
        public int NumberOfInstallments { get; set; }
        public decimal InstallmentAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime NextInstallmentDate { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public string Status { get; set; }
        public virtual Customer Customer { get; set; }
    }
}