using System;

namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO thông tin gói đăng ký hiện tại của người dùng (tên gói, hạn mức token, ngày hết hạn).
    /// Đơn vị hạn mức: token AI. long.MaxValue = không giới hạn.
    /// </summary>
    public class SubscriptionInfoDto
    {
        public string CurrentPlan { get; set; } = "Free";
        public long MonthlyLimit { get; set; }
        public long UsedCount { get; set; }
        public long Remaining { get; set; }
        public DateTime? MonthlyResetDate { get; set; }

        public long ShortTermLimit { get; set; }
        public long ShortTermUsedCount { get; set; }
        public long ShortTermRemaining { get; set; }
        public DateTime? ResetDate { get; set; }

        public DateTime? Expiry { get; set; }
        public bool IsActive { get; set; }

        public long ExtraQuota { get; set; }
        public bool UseExtraQuota { get; set; }
    }
}
