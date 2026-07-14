using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Thực thể Gói mua thêm (Addon Package) cho phép người dùng mua thêm lượt hỏi dự phòng.
    /// Mỗi gói có tên, giá tiền, và số lượng câu hỏi bổ sung (QuotaAmount).
    /// </summary>
    public class AddonPackage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int QuotaAmount { get; set; } // Số lượng câu hỏi mua thêm

        public bool IsActive { get; set; } = true;
    }
}
