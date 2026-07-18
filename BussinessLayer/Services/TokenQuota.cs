namespace BussinessLayer.Services
{
    /// <summary>
    /// Hạn mức token AI theo gói hội viên — nguồn duy nhất cho cả ChatService và SubscriptionService.
    /// Đơn vị: token (usageMetadata của Gemini). long.MaxValue = không giới hạn.
    /// </summary>
    public static class TokenQuota
    {
        /// <summary>Tỷ lệ quy đổi từ "lượt hỏi" cũ sang token (~1 câu hỏi có RAG ≈ 5.000 token).</summary>
        public const long TokensPerLegacyQuestion = 5_000;

        /// <summary>Hạn mức token trong chu kỳ 5 giờ.</summary>
        public static long GetShortTermTokenLimit(string plan) => plan switch
        {
            "Basic" => 50_000,
            "Pro" => 100_000,
            "Ultra" => long.MaxValue,
            _ => 50_000
        };

        /// <summary>Hạn mức token trong tháng (chu kỳ 30 ngày).</summary>
        public static long GetMonthlyTokenLimit(string plan) => plan switch
        {
            "Basic" => 250_000,
            "Pro" => 2_500_000,
            "Ultra" => long.MaxValue,
            _ => 250_000
        };
    }
}
