using System.Collections.Generic;
using System;

namespace BussinessLayer.DTOs
{
    public class ChatRequestDto
    {
        public string Message { get; set; } = string.Empty;
        public int? SessionId { get; set; }
        public List<ChatMessageDto> History { get; set; } = new List<ChatMessageDto>();
        public List<int> SelectedDocIds { get; set; } = new List<int>();
        public bool RestrictToDocs { get; set; } = true;
        public string? ModelName { get; set; }
    }

    public class ChatMessageDto
    {
        public string Role { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public List<CitationDto> Citations { get; set; } = new List<CitationDto>();
    }

    public class ChatSessionDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<ChatMessageDto> Messages { get; set; } = new List<ChatMessageDto>();
    }

    public class ChatSessionActionDto
    {
        public int SessionId { get; set; }
    }

    public class ChatResponseDto
    {
        public bool Success { get; set; }
        public string Reply { get; set; } = string.Empty;
        public string? Message { get; set; }
        public int Remaining { get; set; } = 9999;
        public bool OutOfQuota { get; set; } = false;
        public int? SessionId { get; set; }
        public string? SessionTitle { get; set; }
        public List<CitationDto> Citations { get; set; } = new List<CitationDto>();
    }

    public class CitationDto
    {
        public int DocumentId { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public string? SubjectName { get; set; }
        public string? ChapterTitle { get; set; }
        public int ChunkOrderIndex { get; set; }
        public string Snippet { get; set; } = string.Empty;
        public string FullContent { get; set; } = string.Empty;
    }
}
