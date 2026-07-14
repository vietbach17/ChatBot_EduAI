using System.ComponentModel.DataAnnotations;

namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO tạo mới câu hỏi trắc nghiệm vào Ngân hàng câu hỏi.
    /// </summary>
    public class CreateQuestionDto
    {
        [Required(ErrorMessage = "Vui lòng chọn môn học.")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Nội dung câu hỏi không được để trống.")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại câu hỏi.")]
        public string QuestionType { get; set; } = "MultipleChoice"; // "MultipleChoice", "TrueFalse"

        public string? OptionsJson { get; set; } // JSON array of options: ["A", "B", "C", "D"]

        [Required(ErrorMessage = "Vui lòng chọn đáp án đúng.")]
        public string CorrectAnswer { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn độ khó.")]
        public string Difficulty { get; set; } = "Easy"; // "Easy", "Medium", "Hard"

        public string? Tags { get; set; }

        public bool IsAIGenerated { get; set; } = false;
    }
}
