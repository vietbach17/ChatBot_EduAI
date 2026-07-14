using System;
using System.Collections.Generic;

namespace BussinessLayer.DTOs
{
    /// <summary>DTO dữ liệu làm bài thi: mã lượt làm, tiêu đề, thời gian, giới hạn và danh sách câu hỏi.</summary>
    public class TakeQuizDto
    {
        public int AttemptId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int TimeLimitMinutes { get; set; }
        public List<TakeQuizQuestionDto> Questions { get; set; } = new List<TakeQuizQuestionDto>();
    }

    /// <summary>DTO một câu hỏi khi làm bài: nội dung, loại, các lựa chọn và đáp án sinh viên đã chọn.</summary>
    public class TakeQuizQuestionDto
    {
        public int QuestionBankId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();
        public string? SelectedAnswer { get; set; }
    }

    /// <summary>DTO nộp bài thi: mã lượt làm và danh sách câu trả lời của sinh viên.</summary>
    public class SubmitQuizDto
    {
        public int AttemptId { get; set; }
        public List<StudentAnswerDto> Answers { get; set; } = new List<StudentAnswerDto>();
    }

    /// <summary>DTO câu trả lời của sinh viên cho một câu hỏi cụ thể.</summary>
    public class StudentAnswerDto
    {
        public int QuestionBankId { get; set; }
        public string? SelectedAnswer { get; set; }
    }

    /// <summary>DTO kết quả bài thi: điểm số, số câu đúng, trạng thái và danh sách câu hỏi để xem lại.</summary>
    public class QuizResultDto
    {
        public int AttemptId { get; set; }
        public decimal Score { get; set; }
        public int CorrectCount { get; set; }
        public int TotalQuestions { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? SubmittedAt { get; set; }
        public bool ShowScoreAfterSubmit { get; set; }
        public List<ReviewQuestionDto> ReviewQuestions { get; set; } = new List<ReviewQuestionDto>();
    }

    /// <summary>DTO câu hỏi khi xem lại bài: đáp án sinh viên, đáp án đúng, đúng/sai và giải thích.</summary>
    public class ReviewQuestionDto
    {
        public int QuestionBankId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();
        public string? StudentAnswer { get; set; }
        public string? CorrectAnswer { get; set; }
        public bool? IsCorrect { get; set; } // Null if score is hidden
        public string? Explanation { get; set; }
    }

    /// <summary>DTO thống kê bài thi: tổng lượt làm, điểm trung bình/cao nhất/thấp nhất và danh sách lượt làm.</summary>
    public class QuizStatisticsDto
    {
        public int QuizId { get; set; }
        public int SubjectId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public int TotalAttempts { get; set; }
        public decimal AverageScore { get; set; }
        public decimal HighestScore { get; set; }
        public decimal LowestScore { get; set; }
        public List<QuizAttemptSummaryDto> Attempts { get; set; } = new List<QuizAttemptSummaryDto>();
    }

    /// <summary>DTO tóm tắt một lượt làm bài: tên sinh viên, thời gian bắt đầu/kết thúc, điểm và trạng thái.</summary>
    public class QuizAttemptSummaryDto
    {
        public int AttemptId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal Score { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
