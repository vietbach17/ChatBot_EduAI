using Microsoft.AspNetCore.Http;

namespace PresentationLayer.ViewModels.Lecturer
{
    /// <summary>ViewModel tạo mới chương học cho môn.</summary>
    public class ChapterCreateViewModel
    {
        public string Title { get; set; } = string.Empty;
    }

    /// <summary>ViewModel cập nhật tiêu đề chương học.</summary>
    public class ChapterUpdateViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    /// <summary>ViewModel xóa chương học: lựa chọn giữ lại hay xóa luôn tài liệu bên trong.</summary>
    public class ChapterDeleteViewModel
    {
        public string Option { get; set; } = "delete";
    }

    /// <summary>ViewModel tải lên tài liệu: chương đích, tiêu đề, file và ConnectionId để báo tiến trình xử lý.</summary>
    public class DocumentUploadViewModel
    {
        public int? ChapterId { get; set; }
        public string Title { get; set; } = string.Empty;
        public IFormFile? File { get; set; }
        public List<IFormFile> Files { get; set; } = new();
        public string? ConnectionId { get; set; }
    }

    /// <summary>ViewModel di chuyển tài liệu sang chương khác.</summary>
    public class DocumentMoveViewModel
    {
        public int DocumentId { get; set; }
        public int? ToChapterId { get; set; }
    }

    /// <summary>
    /// ViewModel gộp hiển thị nhật ký hoạt động Tài liệu và Bài thi trên cùng 1 dòng thời gian.
    /// </summary>
    public class ActivityLogItemViewModel
    {
        public DateTime Timestamp { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty; // "Document" | "Quiz"
        public string Action { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int? LinkId { get; set; }
    }
}
