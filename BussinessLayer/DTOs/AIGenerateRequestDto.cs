using System.ComponentModel.DataAnnotations;

namespace BussinessLayer.DTOs
{
    public class AIGenerateRequestDto
    {
        [Required(ErrorMessage = "Vui lòng chọn môn học.")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập chủ đề câu hỏi.")]
        [MinLength(3, ErrorMessage = "Chủ đề phải dài ít nhất 3 ký tự.")]
        public string Topic { get; set; } = string.Empty;

        [Range(1, 10, ErrorMessage = "Chỉ tạo tối đa từ 1 đến 10 câu hỏi trong một lần.")]
        public int Count { get; set; } = 3;

        [Required(ErrorMessage = "Vui lòng chọn độ khó.")]
        public string Difficulty { get; set; } = "Medium"; // "Easy", "Medium", "Hard"

        [Required(ErrorMessage = "Vui lòng chọn loại câu hỏi.")]
        public string QuestionType { get; set; } = "All"; // "All", "MultipleChoice", "TrueFalse"

        public int? DocumentId { get; set; }
    }
}
