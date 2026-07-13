using System;
using System.Collections.Generic;

namespace BussinessLayer.DTOs
{
    public class VariantQuestionsDto
    {
        public int VariantIndex { get; set; }
        public List<int> QuestionIds { get; set; } = new List<int>();
    }

    public class CreateQuizDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SubjectId { get; set; }

        public int TimeLimitMinutes { get; set; } = 15;
        public int MaxAttempts { get; set; } = 1;

        public bool IsShuffled { get; set; } = false; // Đảo vị trí đáp án (A,B,C,D) trong câu hỏi
        
        public int NumVariants { get; set; } = 1; // Số lượng mã đề

        public bool ShowScoreAfterSubmit { get; set; } = true;
        public string GradingMethod { get; set; } = "Highest"; // "Highest", "Average", "Latest"
        public string? AccessCode { get; set; }

        // Danh sách các mã đề và các câu hỏi thuộc mã đề đó
        public List<VariantQuestionsDto> Variants { get; set; } = new List<VariantQuestionsDto>();
    }

    public class QuizDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        public int TimeLimitMinutes { get; set; }
        public int MaxAttempts { get; set; }
        
        public int QuestionsPerAttempt { get; set; } // Số lượng câu hỏi của mã đề
        public int NumVariants { get; set; }

        public string GradingMethod { get; set; } = string.Empty;
        public bool ShowScoreAfterSubmit { get; set; }
        
        public bool HasPassword { get; set; }
        
        public int AttemptsDoneByCurrentUser { get; set; }
    }
}
