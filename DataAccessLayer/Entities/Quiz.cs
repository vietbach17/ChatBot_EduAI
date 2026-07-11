using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
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

        public bool IsShuffled { get; set; } = false;

        public int NumVariants { get; set; } = 1; // Số lượng mã đề chia ra

        public bool ShowScoreAfterSubmit { get; set; } = true;

        public int TimeLimitMinutes { get; set; } = 15; // Thời gian làm bài (phút)

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Draft"; // "Draft", "Open", "Closed"

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
