using System;

namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO hiển thị nhật ký hoạt động bài thi.
    /// </summary>
    public class QuizActivityLogDto
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public int? QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
