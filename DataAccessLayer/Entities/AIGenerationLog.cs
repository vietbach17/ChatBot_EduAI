using System;

namespace DataAccessLayer.Entities
{
    public class AIGenerationLog
    {
        public int Id { get; set; }
        public int LecturerId { get; set; }
        public User Lecturer { get; set; } = null!;
        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;
        public string Topic { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string GeneratedQuestionsJson { get; set; } = string.Empty; // Lưu danh sách câu hỏi dạng JSON
    }
}
