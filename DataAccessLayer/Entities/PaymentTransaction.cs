using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        public int PlanId { get; set; }
        
        [ForeignKey("PlanId")]
        public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

        [Required]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty; // "VNPay", "PayOS", "SePay"

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // "Pending", "Success", "Failed"

        [MaxLength(100)]
        public string? TransactionCode { get; set; } // Mã GD trả về từ Cổng Thanh Toán

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiryDate { get; set; } // Ngày hết hạn dự kiến sau khi thanh toán thành công
    }
}
