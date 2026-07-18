using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer;
using BussinessLayer.IServices;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Entities;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Dịch vụ Sinh câu hỏi trắc nghiệm tự động bằng AI (Gemini). Nhận đầu vào: chủ đề, độ khó, loại câu hỏi, số lượng, và trả về danh sách câu hỏi đã format.
    /// </summary>
    public class AIQuizGeneratorService : IAIQuizGeneratorService
    {
        private readonly IGeminiService _geminiService;
        private readonly ApplicationDbContext _context;

        public AIQuizGeneratorService(IGeminiService geminiService, ApplicationDbContext context)
        {
            _geminiService = geminiService;
            _context = context;
        }

        public async Task<IEnumerable<AIGenerateResultDto>> GenerateQuestionsAsync(AIGenerateRequestDto request, int lecturerId)
        {
            if (request == null) return new List<AIGenerateResultDto>();

            var subject = await _context.Subjects.FindAsync(request.SubjectId);
            var subjectName = subject?.Name ?? "Môn học chung";

            var typeConstraint = "";
            if (request.QuestionType == "MultipleChoice")
            {
                typeConstraint = "Tất cả các câu hỏi phải có loại 'MultipleChoice' (Trắc nghiệm 4 lựa chọn). Không tạo câu hỏi Đúng/Sai.";
            }
            else if (request.QuestionType == "TrueFalse")
            {
                typeConstraint = "Tất cả các câu hỏi phải có loại 'TrueFalse' (Đúng/Sai). Không tạo câu hỏi Trắc nghiệm 4 lựa chọn.";
            }
            else
            {
                typeConstraint = "Bạn có thể tự do tạo kết hợp cả hai loại câu hỏi 'MultipleChoice' và 'TrueFalse' theo tỉ lệ ngẫu nhiên.";
            }

            string documentContext = "";
            if (request.DocumentId.HasValue)
            {
                var doc = await _context.Documents.FindAsync(request.DocumentId.Value);
                if (doc != null && !string.IsNullOrWhiteSpace(doc.Content))
                {
                    documentContext = $"\n\nTÀI LIỆU THAM KHẢO NGUỒN:\n---\n{doc.Content}\n---\n\nYêu cầu quan trọng nhất: Bạn phải tạo câu hỏi dựa trên nội dung tài liệu tham khảo nguồn ở trên. Không được tạo câu hỏi bằng các kiến thức khác nằm ngoài tài liệu này.";
                }
            }

            var prompt = $@"
Bạn là một giảng viên đại học và chuyên gia khảo thí giàu kinh nghiệm.
Hãy tạo {request.Count} câu hỏi tiếng Việt chất lượng cao liên quan đến chủ đề '{request.Topic}' của môn học '{subjectName}'.
Mức độ khó của các câu hỏi phải là: '{request.Difficulty}'.

Yêu cầu về loại câu hỏi:
{typeConstraint}

Yêu cầu chi tiết:
1. Đối với câu hỏi loại 'MultipleChoice' (Trắc nghiệm 4 lựa chọn):
   - Mảng 'options' phải chứa chính xác 4 phần tử tương ứng với 4 đáp án lựa chọn (A, B, C, D).
   - Đáp án đúng 'correctAnswer' phải là một chữ cái in hoa đơn lẻ ('A', 'B', 'C' hoặc 'D') ứng với vị trí phần tử thứ 1, 2, 3, 4 trong mảng 'options'.
2. Đối với câu hỏi loại 'TrueFalse' (Đúng / Sai):
   - Mảng 'options' phải để rỗng hoặc null.
   - Đáp án đúng 'correctAnswer' phải là chuỗi 'True' hoặc 'False'.
3. Nội dung câu hỏi phải thực tế, mang tính học thuật cao, không mơ hồ. Tiếng Việt phải chuẩn xác, có dấu rõ ràng.
4. Trường 'tags' chứa các từ khóa ngăn cách bởi dấu phẩy liên quan đến nội dung câu hỏi.
5. Trường 'difficulty' phải là giá trị '{request.Difficulty}'.{documentContext}
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
                var resultList = questions ?? new List<AIGenerateResultDto>();

                if (resultList.Count > 0)
                {
                    var logEntry = new AIGenerationLog
                    {
                        LecturerId = lecturerId,
                        SubjectId = request.SubjectId,
                        Topic = request.Topic,
                        Difficulty = request.Difficulty,
                        QuestionType = request.QuestionType,
                        Quantity = resultList.Count,
                        CreatedAt = DateTime.UtcNow,
                        GeneratedQuestionsJson = jsonResult
                    };
                    _context.AIGenerationLogs.Add(logEntry);
                    await _context.SaveChangesAsync();
                }

                return resultList;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi khi tạo câu hỏi bằng AI: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<AIGenerationLogDto>> GetGenerationLogsAsync(int lecturerId)
        {
            return await _context.AIGenerationLogs
                .Include(l => l.Subject)
                .Where(l => l.LecturerId == lecturerId)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new AIGenerationLogDto
                {
                    Id = l.Id,
                    SubjectId = l.SubjectId,
                    SubjectCode = l.Subject.Code,
                    SubjectName = l.Subject.Name,
                    Topic = l.Topic,
                    Difficulty = l.Difficulty,
                    QuestionType = l.QuestionType,
                    Quantity = l.Quantity,
                    CreatedAt = l.CreatedAt,
                    GeneratedQuestionsJson = l.GeneratedQuestionsJson
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<AIGenerationLogDto>> GetRecentGenerationLogsAsync(int take)
        {
            return await _context.AIGenerationLogs
                .Include(l => l.Lecturer)
                .Include(l => l.Subject)
                .OrderByDescending(l => l.CreatedAt)
                .Take(take)
                .Select(l => new AIGenerationLogDto
                {
                    Id = l.Id,
                    LecturerUsername = l.Lecturer.Username,
                    SubjectId = l.SubjectId,
                    SubjectCode = l.Subject.Code,
                    SubjectName = l.Subject.Name,
                    Topic = l.Topic,
                    Difficulty = l.Difficulty,
                    QuestionType = l.QuestionType,
                    Quantity = l.Quantity,
                    CreatedAt = l.CreatedAt,
                    GeneratedQuestionsJson = l.GeneratedQuestionsJson
                })
                .ToListAsync();
        }
    }
}
