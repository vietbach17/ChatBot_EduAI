using System;

namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO thông tin gói đăng ký hiện tại của người dùng (tên gói, hạn mức, ngày hết hạn).
    /// </summary>
    public class SubscriptionInfoDto
    {
        public string CurrentPlan { get; set; } = "Free";
        public int MonthlyLimit { get; set; }
        public int UsedCount { get; set; }
        public int Remaining { get; set; }
        public DateTime? MonthlyResetDate { get; set; }

        public int ShortTermLimit { get; set; }
        public int ShortTermUsedCount { get; set; }
        public int ShortTermRemaining { get; set; }
        public DateTime? ResetDate { get; set; }

        public DateTime? Expiry { get; set; }
        public bool IsActive { get; set; }
        
        public int ExtraQuota { get; set; }
        public bool UseExtraQuota { get; set; }
    }
}
