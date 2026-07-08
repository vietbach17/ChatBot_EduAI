using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
    public class ChatSession
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }
        
        [Required]
        public string Title { get; set; } = "Cuộc trò chuyện mới";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
