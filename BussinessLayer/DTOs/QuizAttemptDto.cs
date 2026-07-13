using System;
using System.Collections.Generic;

namespace BussinessLayer.DTOs
{
    public class TakeQuizDto
    {
        public int AttemptId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int TimeLimitMinutes { get; set; }
        public List<TakeQuizQuestionDto> Questions { get; set; } = new List<TakeQuizQuestionDto>();
    }

    public class TakeQuizQuestionDto
    {
        public int QuestionBankId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();
    }

    public class SubmitQuizDto
    {
        public int AttemptId { get; set; }
        public List<StudentAnswerDto> Answers { get; set; } = new List<StudentAnswerDto>();
    }

    public class StudentAnswerDto
    {
        public int QuestionBankId { get; set; }
        public string? SelectedAnswer { get; set; }
    }

    public class QuizResultDto
    {
        public int AttemptId { get; set; }
        public decimal Score { get; set; }
        public int CorrectCount { get; set; }
        public int TotalQuestions { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? SubmittedAt { get; set; }
        public List<ReviewQuestionDto> ReviewQuestions { get; set; } = new List<ReviewQuestionDto>();
    }

    public class ReviewQuestionDto
    {
        public int QuestionBankId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();
        public string? StudentAnswer { get; set; }
        public string? CorrectAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public string? Explanation { get; set; }
    }

    public class QuizStatisticsDto
    {
        public int QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public int TotalAttempts { get; set; }
        public decimal AverageScore { get; set; }
        public decimal HighestScore { get; set; }
        public decimal LowestScore { get; set; }
    }
}
