using System;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Thực thể ghi lại nhật ký hoạt động liên quan đến Tài liệu (Document).
    /// Theo dõi các hành động: Tải lên (Uploaded), Xóa (Deleted), Di chuyển (Moved),
    /// kèm theo thông tin người thực hiện và thời gian.
    /// </summary>
    public class DocumentActivityLog
    {
        [Key]
        public int Id { get; set; }

        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        public int? DocumentId { get; set; }
        
        [Required]
        public string DocumentTitle { get; set; } = string.Empty;

        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty; // e.g. "Uploaded", "Deleted", "Moved"

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
