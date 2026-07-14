using System.Collections.Generic;

namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO hiển thị và cập nhật thông tin Môn học.
    /// </summary>
    public class SubjectDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        
        public bool IsDeleted { get; set; }
        public int? LecturerId { get; set; }
        public string? LecturerName { get; set; }
        
        public List<ChapterDto> Chapters { get; set; } = new List<ChapterDto>();
        public List<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
    }

    public class ChapterDto
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        
        public List<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
    }
}
