using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Student"; // "Admin", "Lecturer", "Student"
        
        public string? Email { get; set; }

        public bool IsDeleted { get; set; } = false;

        // ===== SUBSCRIPTION =====
        [MaxLength(20)]
        public string SubscriptionPlan { get; set; } = "Free"; // "Free", "Basic", "Premium"

        public DateTime? SubscriptionExpiry { get; set; } // null = chÆ°a mua / háº¿t háº¡n

        public int MonthlyQuestionCount { get; set; } = 0; // sá»‘ cÃ¢u Ä‘Ã£ há» i trong thÃ¡ng

        public DateTime? QuotaResetDate { get; set; } // ngày reset quota (đầu tháng tiếp theo)
        // ========================
        
        public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
        public ICollection<Subject> AssignedSubjects { get; set; } = new List<Subject>();
    }
}
