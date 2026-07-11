using System;

namespace BussinessLayer.DTOs
{
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
    }
}
