using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Thực thể ghi lại nhật ký hoạt động liên quan đến Ngân hàng câu hỏi (Question Bank).
    /// Theo dõi các hành động: Created, Edited, Deleted, Restored.
    /// Cho phép xem lại nội dung cũ nếu câu hỏi bị sửa.
    /// </summary>
    public class QuestionBankActivityLog
    {
        [Key]
        public int Id { get; set; }

        public int? QuestionBankId { get; set; } // Nullable phòng trường hợp bị xóa vĩnh viễn
        public QuestionBank? QuestionBank { get; set; }

        [Required]
        public int UserId { get; set; } // Ai là người thực hiện hành động (Lecturer / Admin)
        
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // "Created", "Edited", "Deleted", "Restored"

        /// <summary>
        /// Nội dung tóm tắt của câu hỏi (để hiển thị nhanh, ví dụ snippet 50 ký tự)
        /// </summary>
        public string QuestionSnippet { get; set; } = string.Empty;

        /// <summary>
        /// Toàn bộ nội dung cũ của câu hỏi được Serialize thành chuỗi JSON trước khi bị chỉnh sửa.
        /// Dùng để Admin đối chiếu xem người dùng đã thay đổi điều gì.
        /// Chỉ lưu khi Action = "Edited".
        /// </summary>
        public string? OldContentJson { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
