using Microsoft.AspNetCore.Http;

namespace PresentationLayer.ViewModels.Lecturer
{
    public class ChapterCreateViewModel
    {
        public string Title { get; set; } = string.Empty;
    }

    public class ChapterUpdateViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class ChapterDeleteViewModel
    {
        public string Option { get; set; } = "delete";
    }

    public class DocumentUploadViewModel
    {
        public int? ChapterId { get; set; }
        public string Title { get; set; } = string.Empty;
        public IFormFile? File { get; set; }
        public string? ConnectionId { get; set; }
    }

    public class DocumentMoveViewModel
    {
        public int DocumentId { get; set; }
        public int? ToChapterId { get; set; }
    }
}
