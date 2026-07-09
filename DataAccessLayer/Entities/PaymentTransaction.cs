using System;

namespace DataAccessLayer.Entities
{
    // STUB: This file is a stub for TV1 to implement later.
    // It is created so that TV2's code can compile.
    public class PaymentTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PlanId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } // VNPay, PayOS, SePay
        public string Status { get; set; } // Pending, Success, Failed
        public string TransactionCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public User User { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; }
    }
}
