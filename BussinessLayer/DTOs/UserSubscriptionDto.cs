using System;

namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO thông tin gói đăng ký của người dùng (dành cho Admin quản lý).
    /// </summary>
    public class UserSubscriptionDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Role { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = "Free";
        public DateTime? SubscriptionExpiry { get; set; }
        public long MonthlyTokensUsed { get; set; }
        public long MonthlyLimit { get; set; }
        public DateTime? QuotaResetDate { get; set; }

        public long ShortTermTokensUsed { get; set; }
        public long ShortTermLimit { get; set; }
        public DateTime? ShortTermResetDate { get; set; }

        public bool IsActive { get; set; }
    }
}
