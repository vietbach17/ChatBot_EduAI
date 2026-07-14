using System;
using System.Collections.Generic;

namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết một Báo cáo / Khiếu nại.
    /// </summary>
    public class ReportDto
    {
        public int Id { get; set; }
        public int ReporterId { get; set; }
        public string ReporterName { get; set; } = string.Empty;
        public string ReporterRole { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string CategoryLabel { get; set; } = string.Empty; // nhãn tiếng Việt
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int? RelatedTransactionId { get; set; }
        public string Status { get; set; } = "Pending";
        public string StatusLabel { get; set; } = string.Empty; // nhãn tiếng Việt
        public string? AdminResponse { get; set; }
        public string? HandledByAdminName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>DTO tạo báo cáo mới (từ Student / Lecturer).</summary>
    public class CreateReportDto
    {
        public int ReporterId { get; set; }
        public string ReporterRole { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int? RelatedTransactionId { get; set; }
    }

    /// <summary>DTO admin phản hồi & cập nhật trạng thái báo cáo.</summary>
    public class ReportRespondDto
    {
        public int ReportId { get; set; }
        public int AdminId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AdminResponse { get; set; }
    }

    /// <summary>
    /// Danh mục phân loại báo cáo và nhãn hiển thị, khác nhau theo vai trò người gửi.
    /// </summary>
    public static class ReportCategories
    {
        public static readonly Dictionary<string, string> Student = new()
        {
            { "Transaction", "Giao dịch / Thanh toán" },
            { "TechnicalIssue", "Lỗi kỹ thuật" },
            { "ContentError", "Sai sót nội dung / tài liệu" },
            { "AccountIssue", "Vấn đề tài khoản" },
            { "Other", "Vấn đề khác" }
        };

        public static readonly Dictionary<string, string> Lecturer = new()
        {
            { "SubjectIssue", "Vấn đề môn học / lớp" },
            { "SystemBug", "Lỗi hệ thống" },
            { "ContentError", "Sai sót nội dung / câu hỏi" },
            { "FeatureRequest", "Đề xuất tính năng" },
            { "Other", "Vấn đề khác" }
        };

        public static readonly Dictionary<string, string> Status = new()
        {
            { "Pending", "Chờ xử lý" },
            { "InProgress", "Đang xử lý" },
            { "Resolved", "Đã xử lý" },
            { "Rejected", "Từ chối" }
        };

        /// <summary>Lấy nhãn category theo role, fallback về chính key nếu không tìm thấy.</summary>
        public static string GetCategoryLabel(string role, string category)
        {
            var map = role == "Lecturer" ? Lecturer : Student;
            return map.TryGetValue(category, out var label) ? label : category;
        }

        public static string GetStatusLabel(string status)
            => Status.TryGetValue(status, out var label) ? label : status;

        public static Dictionary<string, string> ForRole(string role)
            => role == "Lecturer" ? Lecturer : Student;
    }
}
