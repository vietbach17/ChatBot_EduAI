using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;

namespace BussinessLayer.Services
{
    public class QuizService : IQuizService
    {
        private readonly IQuizRepository _quizRepo;
        private readonly IQuizAttemptRepository _attemptRepo;

        public QuizService(IQuizRepository quizRepo, IQuizAttemptRepository attemptRepo)
        {
            _quizRepo = quizRepo;
            _attemptRepo = attemptRepo;
        }

        // ==========================================
        // 1. LECTURER: TẠO BÀI QUIZ & CHIA MÃ ĐỀ
        // ==========================================
        public async Task<int> CreateQuizAsync(int lecturerId, CreateQuizDto dto)
        {
            // 1. Khởi tạo đối tượng Quiz
            var quiz = new Quiz
            {
                Title = dto.Title,
                Description = dto.Description,
                SubjectId = dto.SubjectId,
                LecturerId = lecturerId,
                TimeLimitMinutes = dto.TimeLimitMinutes,
                MaxAttempts = dto.MaxAttempts,
                IsShuffled = dto.IsShuffled,
                NumVariants = dto.NumVariants, // Lưu số lượng mã đề
                ShowScoreAfterSubmit = dto.ShowScoreAfterSubmit,
                GradingMethod = dto.GradingMethod,
                AccessCode = dto.AccessCode,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            // Nếu người dùng truyền vào có bao nhiêu câu hỏi ở Variant đầu tiên, 
            // ta lưu lại để phục vụ hiển thị TotalQuestions (giả định các mã đề có cùng số lượng câu hỏi)
            if (dto.Variants.Any())
            {
                quiz.TotalQuestions = dto.Variants.First().QuestionIds.Count;
                
            }

            await _quizRepo.AddAsync(quiz); // Lấy được quiz.Id sau khi lưu

            // 2. Logic Chia Mã Đề (Lưu vào bảng QuizQuestion)
            var quizQuestions = new List<QuizQuestion>();

            foreach (var variant in dto.Variants)
            {
                int order = 1;
                foreach (var qBankId in variant.QuestionIds)
                {
                    quizQuestions.Add(new QuizQuestion
                    {
                        QuizId = quiz.Id,
                        QuestionBankId = qBankId,
                        VariantIndex = variant.VariantIndex, // Đánh dấu câu này thuộc mã đề nào
                        OrderIndex = order++               // Đánh dấu thứ tự câu hỏi
                    });
                }
            }

            // 3. Lưu toàn bộ xuống Database
            if (quizQuestions.Any())
            {
                await _quizRepo.AddQuestionsAsync(quizQuestions);
            }

            return quiz.Id;
        }

        public async Task<QuizStatisticsDto> GetQuizStatisticsAsync(int quizId, int lecturerId)
        {
            var quiz = await _quizRepo.GetByIdAsync(quizId);
            if (quiz == null || quiz.LecturerId != lecturerId)
                throw new Exception("Bài thi không tồn tại hoặc bạn không có quyền truy cập.");

            var attempts = await _attemptRepo.GetAllAttemptsForQuizAsync(quizId);
            var gradedAttempts = attempts.Where(a => a.Status == "Graded").ToList();

            var stats = new QuizStatisticsDto
            {
                QuizId = quiz.Id,
                QuizTitle = quiz.Title,
                TotalAttempts = gradedAttempts.Count,
                AverageScore = 0,
                HighestScore = 0,
                LowestScore = 0
            };

            if (gradedAttempts.Any())
            {
                stats.AverageScore = Math.Round(gradedAttempts.Average(a => a.Score), 2);
                stats.HighestScore = gradedAttempts.Max(a => a.Score);
                stats.LowestScore = gradedAttempts.Min(a => a.Score);
            }

            return stats;
        }

        // ==========================================
        // 2. STUDENT: XEM CHI TIẾT, BẮT ĐẦU VÀ NỘP BÀI
        // ==========================================
        public async Task<QuizDetailDto> GetQuizDetailAsync(int quizId, int studentId)
        {
            var quiz = await _quizRepo.GetByIdAsync(quizId);
            if (quiz == null)
                throw new Exception("Bài thi không tồn tại.");

            var previousAttempts = await _attemptRepo.GetAttemptsByStudentAsync(studentId, quizId);

            return new QuizDetailDto
            {
                Id = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                TimeLimitMinutes = quiz.TimeLimitMinutes,
                MaxAttempts = quiz.MaxAttempts,
                NumVariants = quiz.NumVariants,
                GradingMethod = quiz.GradingMethod,
                ShowScoreAfterSubmit = quiz.ShowScoreAfterSubmit,
                HasPassword = !string.IsNullOrEmpty(quiz.AccessCode),
                AttemptsDoneByCurrentUser = previousAttempts.Count()
            };
        }

        public async Task<TakeQuizDto> StartQuizAsync(int studentId, int quizId, string? accessCode)
        {
            var quiz = await _quizRepo.GetByIdAsync(quizId);
            if (quiz == null)
                throw new Exception("Bài thi không tồn tại.");

            if (quiz.Status != "Open")
                throw new Exception("Bài thi hiện không được mở.");

            if (quiz.StartTime.HasValue && DateTime.UtcNow < quiz.StartTime.Value)
                throw new Exception("Chưa đến giờ làm bài.");

            if (quiz.EndTime.HasValue && DateTime.UtcNow > quiz.EndTime.Value)
                throw new Exception("Đã hết hạn làm bài.");

            if (!string.IsNullOrEmpty(quiz.AccessCode) && quiz.AccessCode != accessCode)
                throw new Exception("Mật khẩu bài thi không chính xác.");

            var previousAttempts = await _attemptRepo.GetAttemptsByStudentAsync(studentId, quizId);
            if (previousAttempts.Count() >= quiz.MaxAttempts)
                throw new Exception("Bạn đã hết số lần làm bài.");

            // 1. Random bốc Mã đề
            int assignedVariant = 1;
            if (quiz.NumVariants > 1)
            {
                assignedVariant = Random.Shared.Next(1, quiz.NumVariants + 1);
            }

            // Lấy danh sách câu hỏi thuộc mã đề đó
            var allQuestions = await _quizRepo.GetQuizQuestionsAsync(quizId);
            var assignedQuestions = allQuestions.Where(q => q.VariantIndex == assignedVariant).ToList();

            if (!assignedQuestions.Any())
                throw new Exception("Mã đề này không có câu hỏi nào. Vui lòng liên hệ giảng viên.");

            // 2. Trộn câu hỏi (nếu cần)
            if (quiz.IsShuffled)
            {
                assignedQuestions = assignedQuestions.OrderBy(x => Random.Shared.Next()).ToList();
            }

            // 3. Khởi tạo Attempt
            var attempt = new QuizAttempt
            {
                QuizId = quizId,
                StudentId = studentId,
                StartTime = DateTime.UtcNow,
                TotalQuestions = assignedQuestions.Count,
                Status = "InProgress"
            };

            await _attemptRepo.CreateAttemptAsync(attempt); // Sinh ra attempt.Id

            // 4. Tạo bộ QuizAnswer để "Chốt cứng" đề thi
            var answers = assignedQuestions.Select(q => new QuizAnswer
            {
                AttemptId = attempt.Id,
                QuestionBankId = q.QuestionBankId,
                SelectedAnswer = null,
                IsCorrect = false
            }).ToList();

            await _attemptRepo.AddAnswersAsync(answers);

            // 5. Build DTO trả về (TUYỆT ĐỐI KHÔNG CÓ CorrectAnswer)
            var dto = new TakeQuizDto
            {
                AttemptId = attempt.Id,
                Title = quiz.Title,
                StartTime = attempt.StartTime,
                TimeLimitMinutes = quiz.TimeLimitMinutes,
                Questions = assignedQuestions.Select(q => new TakeQuizQuestionDto
                {
                    QuestionBankId = q.QuestionBankId,
                    QuestionText = q.QuestionBank?.Content ?? "N/A",
                    Type = q.QuestionBank?.QuestionType ?? "MultipleChoice",
                    Options = GetOptionsList(q.QuestionBank)
                }).ToList()
            };

            return dto;
        }

        private List<string> GetOptionsList(QuestionBank? qb)
        {
            var options = new List<string>();
            if (qb == null || string.IsNullOrEmpty(qb.OptionsJson)) 
                return options;

            try
            {
                var parsedOptions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(qb.OptionsJson);
                if (parsedOptions != null)
                {
                    options = parsedOptions;
                }
            }
            catch
            {
                // Fallback nếu chuỗi JSON bị lỗi
            }

            return options;
        }

        public async Task<QuizResultDto> SubmitQuizAsync(int studentId, SubmitQuizDto dto)
        {
            var attempt = await _attemptRepo.GetAttemptWithAnswersAsync(dto.AttemptId);
            
            if (attempt == null || attempt.StudentId != studentId)
                throw new Exception("Lần làm bài không tồn tại hoặc bạn không có quyền truy cập.");

            if (attempt.Status != "InProgress")
                throw new Exception("Bài thi này đã được nộp hoặc đã xử lý.");

            int correctCount = 0;

            // Chấm điểm từng câu
            foreach (var answerRecord in attempt.Answers)
            {
                // Tìm câu trả lời mà sinh viên gửi lên
                var submittedAnswer = dto.Answers.FirstOrDefault(a => a.QuestionBankId == answerRecord.QuestionBankId);
                
                if (submittedAnswer != null && !string.IsNullOrEmpty(submittedAnswer.SelectedAnswer))
                {
                    answerRecord.SelectedAnswer = submittedAnswer.SelectedAnswer;
                    
                    // So sánh với đáp án đúng trong QuestionBank
                    if (answerRecord.QuestionBank != null && 
                        answerRecord.SelectedAnswer.Trim().Equals(answerRecord.QuestionBank.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        answerRecord.IsCorrect = true;
                        correctCount++;
                    }
                    else
                    {
                        answerRecord.IsCorrect = false;
                    }
                }
                else
                {
                    // Bỏ trống
                    answerRecord.SelectedAnswer = null;
                    answerRecord.IsCorrect = false;
                }
            }

            // Cập nhật điểm (Thang điểm 10)
            attempt.CorrectCount = correctCount;
            if (attempt.TotalQuestions > 0)
            {
                attempt.Score = Math.Round((decimal)correctCount / attempt.TotalQuestions * 10m, 2);
            }
            
            attempt.EndTime = DateTime.UtcNow;
            attempt.Status = "Graded";

            await _attemptRepo.UpdateAttemptAsync(attempt);

            // Trả về kết quả
            return await GetAttemptResultAsync(attempt.Id, studentId);
        }

        public async Task<QuizResultDto> GetAttemptResultAsync(int attemptId, int studentId)
        {
            var attempt = await _attemptRepo.GetAttemptWithAnswersAsync(attemptId);
            
            if (attempt == null || attempt.StudentId != studentId)
                throw new Exception("Lần làm bài không tồn tại hoặc bạn không có quyền truy cập.");

            // Cần lấy thêm config của Quiz để biết có được phép xem đáp án không
            var quiz = await _quizRepo.GetByIdAsync(attempt.QuizId);
            bool showAnswers = quiz?.ShowScoreAfterSubmit ?? true;

            var resultDto = new QuizResultDto
            {
                AttemptId = attempt.Id,
                Score = attempt.Score,
                CorrectCount = attempt.CorrectCount,
                TotalQuestions = attempt.TotalQuestions,
                Status = attempt.Status,
                SubmittedAt = attempt.EndTime,
                ReviewQuestions = attempt.Answers.Select(a => new ReviewQuestionDto
                {
                    QuestionBankId = a.QuestionBankId,
                    QuestionText = a.QuestionBank?.Content ?? "N/A",
                    Options = GetOptionsList(a.QuestionBank),
                    StudentAnswer = a.SelectedAnswer,
                    
                    // Nếu giảng viên cho phép xem điểm/đáp án thì mới trả về
                    CorrectAnswer = showAnswers ? a.QuestionBank?.CorrectAnswer : null,
                    IsCorrect = a.IsCorrect
                    
                }).ToList()
            };

            return resultDto;
        }
    }
}
