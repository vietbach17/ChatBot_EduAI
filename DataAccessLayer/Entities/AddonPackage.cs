using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
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
