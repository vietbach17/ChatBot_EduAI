using System;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Thực thể Tin nhắn trong một Phiên trò chuyện (ChatSession).
    /// Mỗi tin nhắn có Role (user/model), nội dung Text, và có thể kèm theo trích dẫn nguồn tài liệu (CitationPayloadJson).
    /// TokenCount dùng để theo dõi lượng token AI tiêu thụ cho mục đích phân tích.
    /// </summary>
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChatSessionId { get; set; }
        public ChatSession? ChatSession { get; set; }

        [Required]
        public string Role { get; set; } = string.Empty; // "user" hoặc "model"

        // Phân loại tin nhắn: "system" (system prompt), "user", "model" (AI reply)
        [MaxLength(20)]
        public string MessageType { get; set; } = "user";

        [Required]
        public string Text { get; set; } = string.Empty;

        public string? CitationPayloadJson { get; set; }

        // Số token ước tính (dùng cho tracking/analytics)
        public int? TokenCount { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

