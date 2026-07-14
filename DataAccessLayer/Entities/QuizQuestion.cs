using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Thực thể liên kết giữa Bài kiểm tra (Quiz) và Câu hỏi trong Ngân hàng (QuestionBank).
    /// Mỗi bản ghi xác định câu hỏi nào thuộc đề nào (VariantIndex) và thứ tự hiển thị (OrderIndex).
    /// </summary>
    public class QuizQuestion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuizId { get; set; }

        [ForeignKey("QuizId")]
        public Quiz? Quiz { get; set; }

        [Required]
        public int QuestionBankId { get; set; }

        [ForeignKey("QuestionBankId")]
        public QuestionBank? QuestionBank { get; set; }

        public int VariantIndex { get; set; } = 0; // Để phân biệt mã đề (ví dụ đề 1, đề 2)

        public int OrderIndex { get; set; } = 0; // Thứ tự câu hỏi trong đề đó
    }
}
