using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer;
using BussinessLayer.IServices;

namespace BussinessLayer.Services
{
    public class AIQuizGeneratorService : IAIQuizGeneratorService
    {
        private readonly IGeminiService _geminiService;
        private readonly ApplicationDbContext _context;

        public AIQuizGeneratorService(IGeminiService geminiService, ApplicationDbContext context)
        {
            _geminiService = geminiService;
            _context = context;
        }

        public async Task<IEnumerable<AIGenerateResultDto>> GenerateQuestionsAsync(AIGenerateRequestDto request)
        {
            if (request == null) return new List<AIGenerateResultDto>();

            var subject = await _context.Subjects.FindAsync(request.SubjectId);
            var subjectName = subject?.Name ?? "Môn học chung";

            var prompt = $@"
Bạn là một giảng viên đại học và chuyên gia khảo thí giàu kinh nghiệm.
Hãy tạo {request.Count} câu hỏi trắc nghiệm tiếng Việt chất lượng cao liên quan đến chủ đề '{request.Topic}' của môn học '{subjectName}'.
Mức độ khó của các câu hỏi phải là: '{request.Difficulty}'.

Yêu cầu chi tiết:
1. Đối với câu hỏi loại 'MultipleChoice' (Trắc nghiệm 4 lựa chọn):
   - Mảng 'options' phải chứa chính xác 4 phần tử tương ứng với 4 đáp án lựa chọn (A, B, C, D).
   - Đáp án đúng 'correctAnswer' phải là một chữ cái in hoa đơn lẻ ('A', 'B', 'C' hoặc 'D') ứng với vị trí phần tử thứ 1, 2, 3, 4 trong mảng 'options'.
2. Đối với câu hỏi loại 'TrueFalse' (Đúng / Sai):
   - Mảng 'options' phải để rỗng hoặc null.
   - Đáp án đúng 'correctAnswer' phải là chuỗi 'True' hoặc 'False'.
3. Nội dung câu hỏi phải thực tế, mang tính học thuật cao, không mơ hồ. Tiếng Việt phải chuẩn xác, có dấu rõ ràng.
4. Trường 'tags' chứa các từ khóa ngăn cách bởi dấu phẩy liên quan đến nội dung câu hỏi.
5. Trường 'difficulty' phải là giá trị '{request.Difficulty}'.
";

            // Định nghĩa JSON Schema để Gemini trả về định dạng chuẩn 100%
            var schema = @"{
              ""type"": ""ARRAY"",
              ""description"": ""Danh sách câu hỏi được tạo bởi AI."",
              ""items"": {
                ""type"": ""OBJECT"",
                ""properties"": {
                  ""content"": { ""type"": ""STRING"", ""description"": ""Nội dung câu hỏi bằng tiếng Việt."" },
                  ""questionType"": { ""type"": ""STRING"", ""enum"": [""MultipleChoice"", ""TrueFalse""], ""description"": ""Loại câu hỏi: MultipleChoice hoặc TrueFalse."" },
                  ""options"": {
                    ""type"": ""ARRAY"",
                    ""items"": { ""type"": ""STRING"" },
                    ""description"": ""Mảng chứa đúng 4 lựa chọn cho câu trắc nghiệm MultipleChoice. Để trống cho TrueFalse.""
                  },
                  ""correctAnswer"": { ""type"": ""STRING"", ""description"": ""Đáp án đúng. 'A', 'B', 'C', 'D' cho MultipleChoice hoặc 'True', 'False' cho TrueFalse."" },
                  ""difficulty"": { ""type"": ""STRING"", ""enum"": [""Easy"", ""Medium"", ""Hard""], ""description"": ""Độ khó của câu hỏi."" },
                  ""tags"": { ""type"": ""STRING"", ""description"": ""Các thẻ tag liên quan ngăn cách bởi dấu phẩy."" }
                },
                ""required"": [""content"", ""questionType"", ""correctAnswer"", ""difficulty""]
              }
            }";

            try
            {
                var jsonResult = await _geminiService.GenerateJsonContentAsync(prompt, schema);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var questions = JsonSerializer.Deserialize<List<AIGenerateResultDto>>(jsonResult, options);
                return questions ?? new List<AIGenerateResultDto>();
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần và ném ra ngoại lệ thân thiện hơn
                throw new InvalidOperationException($"Lỗi khi tạo câu hỏi bằng AI: {ex.Message}", ex);
            }
        }
    }
}
