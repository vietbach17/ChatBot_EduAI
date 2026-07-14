using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Thực thể Giao dịch Thanh toán. Lưu trữ thông tin mỗi lần người dùng mua gói Subscription hoặc Addon.
    /// Bao gồm: số tiền, phương thức (VNPay/PayOS/SePay), trạng thái (Pending/Success/Failed/Cancelled),
    /// mã giao dịch từ cổng thanh toán, thông tin tài khoản người gửi, và nội dung chuyển khoản thực tế.
    /// </summary>
    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public int? PlanId { get; set; }
        
        [ForeignKey("PlanId")]
        public SubscriptionPlan? SubscriptionPlan { get; set; }

        public int? AddonId { get; set; }

        [ForeignKey("AddonId")]
        public AddonPackage? AddonPackage { get; set; }

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

        [MaxLength(200)]
        public string? SenderAccountInfo { get; set; } // Tài khoản ngân hàng người gửi (nếu có từ Webhook)

        [MaxLength(500)]
        public string? ActualTransferContent { get; set; } // Nội dung chuyển khoản thực tế (từ Webhook)
    }
}
