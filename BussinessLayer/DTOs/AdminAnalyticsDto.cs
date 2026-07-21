using System;

namespace BussinessLayer.DTOs
{
    public class TokenStatsDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int TotalTokens { get; set; }
    }

    public class RevenueStatsDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class UserAnalyticsDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = string.Empty;
        public int TotalTokensUsed { get; set; }
        public decimal TotalAmountSpent { get; set; }
    }
}
