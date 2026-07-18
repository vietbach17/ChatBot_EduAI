using System.Collections.Generic;

namespace BussinessLayer.DTOs
{
    /// <summary>Một điểm dữ liệu thống kê token theo thời gian (tháng hoặc năm).</summary>
    public class TokenStatPointDto
    {
        public string Label { get; set; } = string.Empty; // "01/2026" hoặc "2026"
        public long PromptTokens { get; set; }
        public long OutputTokens { get; set; }
        public long TotalTokens { get; set; }
        public int CallCount { get; set; }
    }

    /// <summary>Một điểm dữ liệu thống kê doanh thu theo thời gian (tháng hoặc năm).</summary>
    public class PaymentStatPointDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
    }

    /// <summary>Chi tiết tiêu thụ của một người dùng: token đã dùng + tiền đã chi mua gói.</summary>
    public class UserUsageStatDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Role { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = string.Empty;
        public long PromptTokens { get; set; }
        public long OutputTokens { get; set; }
        public long TotalTokens { get; set; }
        public int ChatCallCount { get; set; }
        public decimal TotalSpent { get; set; }
        public int PaymentCount { get; set; }
    }

    /// <summary>Toàn bộ dữ liệu trang Thống kê Admin.</summary>
    public class AdminStatisticsDto
    {
        public int SelectedYear { get; set; }
        public List<int> AvailableYears { get; set; } = new();

        public List<TokenStatPointDto> TokenByMonth { get; set; } = new();   // 12 tháng của năm được chọn
        public List<TokenStatPointDto> TokenByYear { get; set; } = new();    // tất cả các năm có dữ liệu

        public List<PaymentStatPointDto> PaymentByMonth { get; set; } = new();
        public List<PaymentStatPointDto> PaymentByYear { get; set; } = new();

        public List<UserUsageStatDto> Users { get; set; } = new();
    }
}
