namespace PresentationLayer.ViewModels.Lecturer
{
    /// <summary>ViewModel một câu hỏi do AI sinh ra, kèm cờ chọn để giảng viên duyệt trước khi lưu.</summary>
    public class SelectedQuestionViewModel
    {
        public bool IsSelected { get; set; }
        public int SubjectId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "MultipleChoice";
        public string? OptionsJson { get; set; }
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Difficulty { get; set; } = "Medium";
        public string? Tags { get; set; }
    }
}
