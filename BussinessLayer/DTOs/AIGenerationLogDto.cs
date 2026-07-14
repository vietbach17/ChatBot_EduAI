using System;

namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO hiển thị lịch sử một lần Giảng viên sinh câu hỏi bằng AI.
    /// </summary>
    public class AIGenerationLogDto
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public string GeneratedQuestionsJson { get; set; } = string.Empty;
    }
}
