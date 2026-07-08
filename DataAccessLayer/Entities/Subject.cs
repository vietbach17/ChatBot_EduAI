using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
    public class Subject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty; // Ví dụ: PRN211, PRN221
    }
}
