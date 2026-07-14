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
                quiz.QuestionsPerAttempt = quiz.TotalQuestions;
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

        public Task<QuizStatisticsDto> GetQuizStatisticsAsync(int quizId, int lecturerId)
        {
            // TODO: Implement Logic tính điểm trung bình (sẽ làm sau)
            throw new NotImplementedException();
        }

        // ==========================================
        // 2. STUDENT: XEM CHI TIẾT, BẮT ĐẦU VÀ NỘP BÀI
        // ==========================================
        public Task<QuizDetailDto> GetQuizDetailAsync(int quizId, int studentId)
        {
            // TODO
            throw new NotImplementedException();
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
            if (qb == null) return options;

            if (!string.IsNullOrEmpty(qb.OptionA)) options.Add(qb.OptionA);
            if (!string.IsNullOrEmpty(qb.OptionB)) options.Add(qb.OptionB);
            if (!string.IsNullOrEmpty(qb.OptionC)) options.Add(qb.OptionC);
            if (!string.IsNullOrEmpty(qb.OptionD)) options.Add(qb.OptionD);

            return options;
        }

        public Task<QuizResultDto> SubmitQuizAsync(int studentId, SubmitQuizDto dto)
        {
            // TODO
            throw new NotImplementedException();
        }

        public Task<QuizResultDto> GetAttemptResultAsync(int attemptId, int studentId)
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
