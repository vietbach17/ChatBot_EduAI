using System;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Thực thể Báo cáo / Khiếu nại (Support Ticket).
    /// Sinh viên và Giảng viên gửi báo cáo về các vấn đề (giao dịch, lỗi kỹ thuật, sai sót nội dung...),
    /// Admin tiếp nhận, phản hồi và xử lý.
    /// </summary>
    public class Report
    {
        [Key]
        public int Id { get; set; }

        // Người gửi báo cáo
        public int ReporterId { get; set; }
        public User? Reporter { get; set; }

        [Required]
        [MaxLength(20)]
        public string ReporterRole { get; set; } = string.Empty; // "Student" | "Lecturer"

        // Phân loại báo cáo (khác nhau theo role) - xem ReportCategories trong tầng nghiệp vụ
        [Required]
        [MaxLength(40)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        // Ảnh đính kèm (vd: ảnh chụp biên lai chuyển khoản cho báo cáo giao dịch)
        public string? ImageUrl { get; set; }

        // Liên kết tới giao dịch (nếu là báo cáo về giao dịch)
        public int? RelatedTransactionId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // "Pending" | "InProgress" | "Resolved" | "Rejected"

        // Phản hồi & xử lý của admin
        public string? AdminResponse { get; set; }
        public int? HandledByAdminId { get; set; }
        public User? HandledByAdmin { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
