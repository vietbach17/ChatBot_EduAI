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

        public Task<TakeQuizDto> StartQuizAsync(int studentId, int quizId, string? accessCode)
        {
             // TODO: Logic Random gán VariantIndex cho sinh viên sẽ nằm ở đây
            throw new NotImplementedException();
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
