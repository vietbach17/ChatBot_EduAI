using System;

namespace BussinessLayer.DTOs
{
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
    }

    public enum DocumentStatus
    {
        Processing = 0,
        Indexed = 1,
        Error = 2
    }
}
