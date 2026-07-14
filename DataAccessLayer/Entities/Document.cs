using System;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Thực thể Tài liệu được Giảng viên tải lên.
    /// Hỗ trợ các định dạng PDF, DOCX, PPTX. Sau khi tải lên, hệ thống sẽ trích xuất nội dung văn bản (Content),
    /// tạo Embedding AI và đánh dấu trạng thái "Indexed" để Sinh viên có thể sử dụng hỏi đáp với Chatbot.
    /// </summary>
    public class Document
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string FileType { get; set; } = string.Empty; // "pdf", "docx", "pptx"
        
        public string? Content { get; set; } // Trích xuất nội dung văn bản
        
        [Required]
        public string Status { get; set; } = "Indexed"; // Trạng thái xử lý (e.g. Indexed, Processing)
        
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
        public string? FileUrl { get; set; } // Đường dẫn file để tải/xem
        
        public int? UploaderId { get; set; }
        public User? Uploader { get; set; }
        
        public int? SubjectId { get; set; }
        public Subject? Subject { get; set; }
        
        public int? ChapterId { get; set; }
        public Chapter? Chapter { get; set; }
        
        public bool IsDeleted { get; set; } = false;
    }
}
