using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Thực thể Bài kiểm tra (Quiz) do Giảng viên tạo từ Ngân hàng câu hỏi.
    /// Hỗ trợ: trộn đề (IsShuffled), chia mã đề (NumVariants), giới hạn thời gian (TimeLimitMinutes),
    /// và quản lý trạng thái (Draft/Open/Closed) cùng thời gian mở/đóng bài thi.
    /// </summary>
    public class Quiz
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }

        [Required]
        public int LecturerId { get; set; }

        [ForeignKey("LecturerId")]
        public User? Lecturer { get; set; }

        public int TotalQuestions { get; set; } = 0;

        //public int QuestionsPerAttempt { get; set; } = 0;

        public bool IsShuffled { get; set; } = false;

        public int NumVariants { get; set; } = 1; // Số lượng mã đề chia ra

        public bool ShowScoreAfterSubmit { get; set; } = true;

        public int TimeLimitMinutes { get; set; } = 15; // Thời gian làm bài (phút)

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Draft"; // "Draft", "Open", "Closed"

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lần làm bài tối đa phải lớn hơn 0")]
        public int MaxAttempts { get; set; } = 1;

        [MaxLength(255)]
        public string? AccessCode { get; set; } // Hashed password to access the quiz

        [Required]
        [MaxLength(20)]
        public string GradingMethod { get; set; } = "Highest"; // "Highest", "Average", "Latest"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
