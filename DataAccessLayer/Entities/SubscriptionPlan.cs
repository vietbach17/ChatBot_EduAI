using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Thực thể Gói đăng ký (Subscription Plan) định nghĩa các mức dịch vụ (Basic/Pro/Premium).
    /// Mỗi gói có giá tiền, giới hạn câu hỏi hàng tháng, thời hạn (DurationDays),
    /// và danh sách tính năng (Features) lưu dưới dạng JSON string.
    /// </summary>
    public class SubscriptionPlan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty; // "Free", "Basic", "Premium", ...

        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; } = 0; // đơn vị VNĐ/tháng

        public int MonthlyQuestionLimit { get; set; } = 5; // -1 = không giới hạn

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0; // thứ tự hiển thị

        [System.ComponentModel.DataAnnotations.Required]
        public string Features { get; set; } = "[]"; // Lưu dạng JSON string, VD: ["Hỏi đáp AI", "Tốc độ nhanh"]

        public int DurationDays { get; set; } = 30;  // Thời hạn của gói (mặc định 30 ngày)

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<string> FeatureList 
        {
            get 
            {
                try 
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(Features) ?? new List<string>();
                }
                catch 
                {
                    return new List<string>();
                }
            }
        }
    }
}
