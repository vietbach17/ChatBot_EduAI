using System;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Thực thể ghi lại nhật ký hoạt động liên quan đến Bài thi (Quiz).
    /// Theo dõi các hành động: Tạo mới (Created), Cập nhật (Updated), Xóa (Deleted),
    /// kèm theo thông tin người thực hiện và thời gian.
    /// </summary>
    public class QuizActivityLog
    {
        [Key]
        public int Id { get; set; }

        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        public int? QuizId { get; set; }

        [Required]
        public string QuizTitle { get; set; } = string.Empty;

        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty; // e.g. "Created", "Updated", "Deleted"

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
