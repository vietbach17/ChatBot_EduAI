using System;

namespace BussinessLayer.DTOs
{
    public class PaymentTransactionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? PlanId { get; set; }
        public int? AddonId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string? TransactionCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
