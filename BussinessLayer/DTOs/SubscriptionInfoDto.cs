using System;

namespace BussinessLayer.DTOs
{
    public class SubscriptionInfoDto
    {
        public string CurrentPlan { get; set; } = "Free";
        public int MonthlyLimit { get; set; }
        public int UsedCount { get; set; }
        public int Remaining { get; set; }
        public DateTime? Expiry { get; set; }
        public bool IsActive { get; set; }
        public DateTime? ResetDate { get; set; }
    }
}
