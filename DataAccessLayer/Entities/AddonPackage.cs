using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Thực thể Gói mua thêm (Addon Package) cho phép người dùng mua thêm token dự phòng.
    /// Mỗi gói có tên, giá tiền, và số token bổ sung (QuotaAmount).
    /// </summary>
    public class AddonPackage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int QuotaAmount { get; set; } // Số token mua thêm

        public bool IsActive { get; set; } = true;
    }
}
