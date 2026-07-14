using System;

namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO hiển thị thông tin Tài liệu (tên, loại file, trạng thái, môn học).
    /// </summary>
    public class DocumentDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public DocumentStatus Status { get; set; }
        public string? FileUrl { get; set; }
        public int? SubjectId { get; set; }
        public int? ChapterId { get; set; }
        public string? SubjectName { get; set; }
        public string? ChapterTitle { get; set; }
        public int? UploaderId { get; set; }
        public string? UploaderName { get; set; }
        public System.DateTime UploadedAt { get; set; }
        public string? Content { get; set; }
        public bool IsDeleted { get; set; }
    }

    /// <summary>Trạng thái xử lý tài liệu: Đang xử lý, Đã lập chỉ mục (embedding), hoặc Lỗi.</summary>
    public enum DocumentStatus
    {
        Processing = 0,
        Indexed = 1,
        Error = 2
    }
}
