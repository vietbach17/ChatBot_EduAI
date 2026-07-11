using System.Collections.Generic;

namespace BussinessLayer.DTOs
{
    public class AIGenerateResultDto
    {
        public string Content { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "MultipleChoice"; // "MultipleChoice", "TrueFalse"
        public List<string> Options { get; set; } = new List<string>();
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Difficulty { get; set; } = "Medium";
        public string Tags { get; set; } = string.Empty;
    }
}
