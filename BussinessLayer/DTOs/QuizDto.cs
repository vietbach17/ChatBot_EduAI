using System;
using System.Collections.Generic;

namespace BussinessLayer.DTOs
{
    /// <summary>DTO một mã đề: số thứ tự mã đề và danh sách Id câu hỏi thuộc mã đề đó.</summary>
    public class VariantQuestionsDto
    {
        public int VariantIndex { get; set; }
        public List<int> QuestionIds { get; set; } = new List<int>();
    }

    public class QuizQuestionDetailDto
    {
        public int QuestionId { get; set; }
        public int VariantIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();
    }

    /// <summary>DTO tạo bài thi mới: cấu hình thời gian, số lần thi, mã đề, cách tính điểm và mật khẩu.</summary>
    public class CreateQuizDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SubjectId { get; set; }

        public int TimeLimitMinutes { get; set; } = 15;
        public int MaxAttempts { get; set; } = 1;

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public bool IsShuffled { get; set; } = false; // Đảo vị trí đáp án (A,B,C,D) trong câu hỏi
        
        public int NumVariants { get; set; } = 1; // Số lượng mã đề

        public bool ShowScoreAfterSubmit { get; set; } = true;
        public string ScoreDisplayTiming { get; set; } = "Immediately";
        public string GradingMethod { get; set; } = "Highest"; // "Highest", "Average", "Latest"
        public string? AccessCode { get; set; }

        // Danh sách các mã đề và các câu hỏi thuộc mã đề đó
        public List<VariantQuestionsDto> Variants { get; set; } = new List<VariantQuestionsDto>();
    }

    /// <summary>DTO cập nhật thông tin bài thi hiện có.</summary>
    public class UpdateQuizDto
    {
        public int SubjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TimeLimitMinutes { get; set; } = 15;
        public int MaxAttempts { get; set; } = 1;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsShuffled { get; set; } = false;
        public bool ShowScoreAfterSubmit { get; set; } = true;
        public string ScoreDisplayTiming { get; set; } = "Immediately";
        public string GradingMethod { get; set; } = "Highest";
        public string? AccessCode { get; set; }
    }

    /// <summary>DTO chi tiết bài thi hiển thị cho sinh viên trước khi làm: cấu hình và lịch sử các lượt làm.</summary>
    public class QuizDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        public int TimeLimitMinutes { get; set; }
        public int MaxAttempts { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        
        public int QuestionsPerAttempt { get; set; } // Số lượng câu hỏi của mã đề
        public int NumVariants { get; set; }

        public string GradingMethod { get; set; } = string.Empty;
        public bool ShowScoreAfterSubmit { get; set; }
        public string ScoreDisplayTiming { get; set; } = string.Empty;
        
        public bool HasPassword { get; set; }
        
        public decimal? FinalScore { get; set; }

        public int AttemptsDoneByCurrentUser { get; set; }
        public List<QuizAttemptSummaryDto> Attempts { get; set; } = new List<QuizAttemptSummaryDto>();
    }

    /// <summary>DTO bài thi trong danh sách của sinh viên: thông tin cơ bản, số lần đã làm và trạng thái.</summary>
    public class StudentQuizDto
    {
        public int QuizId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int TimeLimitMinutes { get; set; }
        public int MaxAttempts { get; set; }
        public int AttemptsCount { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty; // "Chưa làm", "Đang làm", "Hoàn thành", "Hết hạn"
        public bool HasPassword { get; set; }
        public int? LatestAttemptId { get; set; }
    }

    /// <summary>DTO tóm tắt bài thi hiển thị trong danh sách quản lý của giảng viên.</summary>
    public class QuizSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public int TimeLimitMinutes { get; set; }
        public int NumVariants { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
