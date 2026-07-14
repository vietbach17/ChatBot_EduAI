using System;

namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO hiển thị câu hỏi trong Ngân hàng câu hỏi.
    /// </summary>
    public class QuestionBankDto
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty; // "MultipleChoice", "TrueFalse"
        public string? OptionsJson { get; set; }
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty; // "Easy", "Medium", "Hard"
        public string? Tags { get; set; }
        public bool IsAIGenerated { get; set; }
        public int LecturerId { get; set; }
        public string LecturerUsername { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
