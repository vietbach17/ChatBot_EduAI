using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    public class QuizAnswer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AttemptId { get; set; }

        [ForeignKey("AttemptId")]
        public QuizAttempt? Attempt { get; set; }

        [Required]
        public int QuestionBankId { get; set; }

        [ForeignKey("QuestionBankId")]
        public QuestionBank? QuestionBank { get; set; }

        [MaxLength(100)]
        public string? SelectedAnswer { get; set; } // The student's choice ("A", "B", "True", etc.)

        public bool IsCorrect { get; set; } = false;
    }
}
