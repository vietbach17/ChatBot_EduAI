using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Thực thể Lượt làm bài thi của sinh viên. Lưu thông tin mã đề được gán, thời gian bắt đầu/kết thúc,
    /// điểm số, trạng thái (Đang làm/Đã chấm) và danh sách câu trả lời.
    /// </summary>
    public class QuizAttempt
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuizId { get; set; }

        [ForeignKey("QuizId")]
        public Quiz? Quiz { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public User? Student { get; set; }

        [Required]
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        public DateTime? EndTime { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Score { get; set; } = 0;

        public int TotalQuestions { get; set; } = 0; // Number of questions given in this attempt

        public int CorrectCount { get; set; } = 0;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "InProgress"; // "InProgress", "Submitted", "Graded"

        public ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();
    }
}
