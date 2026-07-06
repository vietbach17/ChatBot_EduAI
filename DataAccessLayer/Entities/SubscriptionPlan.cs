using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
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
    }
}
