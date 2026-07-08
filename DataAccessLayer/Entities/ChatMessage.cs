using System;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ChatSessionId { get; set; }
        public ChatSession? ChatSession { get; set; }
        
        [Required]
        public string Role { get; set; } = string.Empty; // "user" hoặc "model"
        
        [Required]
        public string Text { get; set; } = string.Empty;

        public string? CitationPayloadJson { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
