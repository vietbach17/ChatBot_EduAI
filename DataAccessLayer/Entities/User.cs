using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Thực thể Người dùng trong hệ thống.
    /// Lưu trữ thông tin tài khoản, vai trò (Admin/Lecturer/Student), 
    /// gói đăng ký (Subscription), hạn mức câu hỏi (Quota), và mã OTP đặt lại mật khẩu.
    /// </summary>
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Student"; // "Admin", "Lecturer", "Student"
        
        public string? Email { get; set; }

        public bool IsDeleted { get; set; } = false;

        // ===== SUBSCRIPTION =====
        [MaxLength(20)]
        public string SubscriptionPlan { get; set; } = "Basic"; // Mặc định là Basic (Miễn phí)

        public DateTime? SubscriptionExpiry { get; set; } // null = chưa mua / hết hạn

        public int MonthlyQuestionCount { get; set; } = 0; // số câu đã hỏi trong tháng

        public DateTime? QuotaResetDate { get; set; } // ngày reset quota (đầu tháng tiếp theo)

        public int ShortTermQuestionCount { get; set; } = 0; // số câu đã hỏi trong chu kỳ 5 giờ

        public DateTime? ShortTermResetDate { get; set; } // thời điểm reset chu kỳ 5 giờ
        
        public int ExtraQuestionQuota { get; set; } = 0; // số lượt hỏi dự phòng mua thêm
        
        public bool UseExtraQuota { get; set; } = false; // bật tắt việc tiêu thụ lượt dự phòng
        // ========================
        
        // ===== FORGOT PASSWORD =====
        [MaxLength(6)]
        public string? ResetOtp { get; set; }
        
        public DateTime? ResetOtpExpiry { get; set; }
        // ===========================

        
        public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
        public ICollection<Subject> AssignedSubjects { get; set; } = new List<Subject>();
        public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    }
}
