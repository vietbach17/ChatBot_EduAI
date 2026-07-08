using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    public class QuestionBank
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string QuestionType { get; set; } = "MultipleChoice"; // "MultipleChoice", "TrueFalse"

        public string? OptionsJson { get; set; } // JSON array of options: ["A...", "B...", "C...", "D..."]

        [Required]
        [MaxLength(100)]
        public string CorrectAnswer { get; set; } = string.Empty; // "A", "B", "C", "D" or "True", "False"

        [Required]
        [MaxLength(10)]
        public string Difficulty { get; set; } = "Easy"; // "Easy", "Medium", "Hard"

        public string? Tags { get; set; } // Comma-separated values

        public bool IsAIGenerated { get; set; } = false;

        [Required]
        public int LecturerId { get; set; }

        [ForeignKey("LecturerId")]
        public User? Lecturer { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
