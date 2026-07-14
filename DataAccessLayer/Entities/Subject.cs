using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Thực thể Môn học. Mỗi môn học có mã (Code), tên, và được gán cho một Giảng viên phụ trách.
    /// Chứa danh sách các Chương (Chapter) và Tài liệu (Document) thuộc môn học đó.
    /// Hỗ trợ xóa mềm (Soft Delete) thông qua cờ IsDeleted.
    /// </summary>
    public class Subject
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        public bool IsDeleted { get; set; } = false;
        
        public int? LecturerId { get; set; }
        public User? Lecturer { get; set; }
        
        public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
