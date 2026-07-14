using System.Collections.Generic;
using System;

namespace BussinessLayer.DTOs
{
    /// <summary>DTO yêu cầu gửi tin nhắn chat: câu hỏi, phiên chat, tài liệu được chọn và model AI.</summary>
    public class ChatRequestDto
    {
        public string Message { get; set; } = string.Empty;
        public int? SessionId { get; set; }
        public List<ChatMessageDto> History { get; set; } = new List<ChatMessageDto>();
        public List<int> SelectedDocIds { get; set; } = new List<int>();
        public bool RestrictToDocs { get; set; } = true;
        public string? ModelName { get; set; }
    }

    /// <summary>DTO một tin nhắn trong hội thoại: vai trò (người dùng/AI), nội dung, thời gian và nguồn trích dẫn.</summary>
    public class ChatMessageDto
    {
        public string Role { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public List<CitationDto> Citations { get; set; } = new List<CitationDto>();
    }

    /// <summary>DTO một phiên trò chuyện: tiêu đề, thời điểm tạo và danh sách tin nhắn.</summary>
    public class ChatSessionDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<ChatMessageDto> Messages { get; set; } = new List<ChatMessageDto>();
    }

    /// <summary>DTO thao tác trên phiên chat (xóa / xóa nội dung) theo Id phiên.</summary>
    public class ChatSessionActionDto
    {
        public int SessionId { get; set; }
    }

    /// <summary>DTO phản hồi chat: câu trả lời AI, trạng thái, số lượt hỏi còn lại và nguồn trích dẫn.</summary>
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

    /// <summary>DTO nguồn trích dẫn: tài liệu, môn, chương và đoạn nội dung mà AI dùng để trả lời.</summary>
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
