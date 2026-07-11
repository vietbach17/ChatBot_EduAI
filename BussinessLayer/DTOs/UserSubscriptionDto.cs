using System;

namespace BussinessLayer.DTOs
{
    public class UserSubscriptionDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Role { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = "Free";
        public DateTime? SubscriptionExpiry { get; set; }
        public int MonthlyQuestionCount { get; set; }
        public int MonthlyLimit { get; set; }
        public DateTime? QuotaResetDate { get; set; }

        public int ShortTermQuestionCount { get; set; }
        public int ShortTermLimit { get; set; }
        public DateTime? ShortTermResetDate { get; set; }

        public bool IsActive { get; set; }
    }
}
