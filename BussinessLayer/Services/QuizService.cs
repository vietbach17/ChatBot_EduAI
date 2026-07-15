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
    /// <summary>
    /// Dịch vụ Quản lý Bài thi. Xử lý nghiệp vụ tạo/sửa/xóa bài thi và chia mã đề cho Giảng viên,
    /// cùng luồng làm bài của Sinh viên: bắt đầu, lưu tiến trình, nộp bài và chấm điểm.
    /// </summary>
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
                StartTime = dto.StartTime?.ToUniversalTime(),
                EndTime = dto.EndTime?.ToUniversalTime(),
                IsShuffled = dto.IsShuffled,
                NumVariants = dto.NumVariants, // Lưu số lượng mã đề
                ShowScoreAfterSubmit = dto.ShowScoreAfterSubmit,
                ScoreDisplayTiming = dto.ScoreDisplayTiming,
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

        public async Task<QuizStatisticsDto> GetQuizStatisticsAsync(int quizId, int lecturerId, bool isAdmin = false)
        {
            var quiz = await _quizRepo.GetByIdIncludeDeletedAsync(quizId);
            if (quiz == null || (!isAdmin && quiz.LecturerId != lecturerId))
                throw new Exception("Bài thi không tồn tại hoặc bạn không có quyền truy cập.");

            var attempts = await _attemptRepo.GetAllAttemptsForQuizAsync(quizId);
            var gradedAttempts = attempts.Where(a => a.Status == "Graded").ToList();

            var stats = new QuizStatisticsDto
            {
                QuizId = quiz.Id,
                SubjectId = quiz.SubjectId,
                QuizTitle = quiz.Title,
                TotalAttempts = gradedAttempts.Count,
                AverageScore = 0,
                HighestScore = 0,
                LowestScore = 0,
                Attempts = attempts.Select(a => new QuizAttemptSummaryDto
                {
                    AttemptId = a.Id,
                    StudentName = a.Student?.Username ?? "Unknown",
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    Score = a.Score,
                    Status = a.Status
                }).ToList()
            };

            if (gradedAttempts.Any())
            {
                stats.AverageScore = Math.Round(gradedAttempts.Average(a => a.Score), 2);
                stats.HighestScore = gradedAttempts.Max(a => a.Score);
                stats.LowestScore = gradedAttempts.Min(a => a.Score);
            }

            return stats;
        }

        public async Task<UpdateQuizDto> GetQuizForUpdateAsync(int lecturerId, int quizId, bool isAdmin = false)
        {
            var quiz = await _quizRepo.GetByIdAsync(quizId);
            if (quiz == null || (!isAdmin && quiz.LecturerId != lecturerId))
                throw new Exception("Bài thi không tồn tại hoặc bạn không có quyền cập nhật.");

            return new UpdateQuizDto
            {
                SubjectId = quiz.SubjectId,
                Title = quiz.Title,
                Description = quiz.Description,
                TimeLimitMinutes = quiz.TimeLimitMinutes,
                MaxAttempts = quiz.MaxAttempts,
                StartTime = quiz.StartTime?.ToLocalTime(),
                EndTime = quiz.EndTime?.ToLocalTime(),
                IsShuffled = quiz.IsShuffled,
                ShowScoreAfterSubmit = quiz.ShowScoreAfterSubmit,
                ScoreDisplayTiming = quiz.ScoreDisplayTiming,
                GradingMethod = quiz.GradingMethod,
                AccessCode = quiz.AccessCode
            };
        }

        public async Task<List<QuizQuestionDetailDto>> GetQuizQuestionsDetailAsync(int quizId, int lecturerId, bool isAdmin = false)
        {
            var quiz = await _quizRepo.GetByIdAsync(quizId);
            if (quiz == null || (!isAdmin && quiz.LecturerId != lecturerId))
                throw new Exception("Bài thi không tồn tại hoặc bạn không có quyền truy cập.");

            var allQuestions = await _quizRepo.GetQuizQuestionsAsync(quizId);
            var result = new List<QuizQuestionDetailDto>();

            foreach (var q in allQuestions)
            {
                if (q.QuestionBank == null) continue;

                var options = new List<string>();
                if (!string.IsNullOrEmpty(q.QuestionBank.OptionsJson))
                {
                    try { options = System.Text.Json.JsonSerializer.Deserialize<List<string>>(q.QuestionBank.OptionsJson) ?? new List<string>(); }
                    catch { }
                }

                result.Add(new QuizQuestionDetailDto
                {
                    QuestionId = q.QuestionBankId,
                    VariantIndex = q.VariantIndex,
                    Content = q.QuestionBank.Content,
                    QuestionType = q.QuestionBank.QuestionType,
                    Difficulty = q.QuestionBank.Difficulty,
                    CorrectAnswer = q.QuestionBank.CorrectAnswer ?? "",
                    // Ngân hàng câu hỏi hiện chưa lưu lời giải thích -> để trống.
                    Explanation = string.Empty,
                    Options = options
                });
            }

            return result;
        }

        public async Task UpdateQuizAsync(int lecturerId, int quizId, UpdateQuizDto dto, bool isAdmin = false)
        {
            var quiz = await _quizRepo.GetByIdAsync(quizId);
            if (quiz == null || (!isAdmin && quiz.LecturerId != lecturerId))
                throw new Exception("Bài thi không tồn tại hoặc bạn không có quyền cập nhật.");

            quiz.Title = dto.Title;
            quiz.Description = dto.Description;
            quiz.TimeLimitMinutes = dto.TimeLimitMinutes;
            quiz.MaxAttempts = dto.MaxAttempts;
            quiz.StartTime = dto.StartTime?.ToUniversalTime();
            quiz.EndTime = dto.EndTime?.ToUniversalTime();
            quiz.IsShuffled = dto.IsShuffled;
            quiz.ShowScoreAfterSubmit = dto.ShowScoreAfterSubmit;
            quiz.ScoreDisplayTiming = dto.ScoreDisplayTiming;
            quiz.GradingMethod = dto.GradingMethod;
            quiz.AccessCode = dto.AccessCode;

            await _quizRepo.UpdateAsync(quiz);
        }

        public async Task DeleteQuizAsync(int lecturerId, int quizId, bool isAdmin = false)
        {
            var quiz = await _quizRepo.GetByIdAsync(quizId);
            if (quiz == null || (!isAdmin && quiz.LecturerId != lecturerId))
                throw new Exception("Bài thi không tồn tại hoặc bạn không có quyền xóa.");

            await _quizRepo.DeleteAsync(quizId);
        }

        // ==========================================
        // 2. STUDENT: DANH SÁCH, XEM CHI TIẾT, BẮT ĐẦU VÀ NỘP BÀI
        // ==========================================
        public async Task<List<StudentQuizDto>> GetStudentQuizzesAsync(int studentId)
        {
            var activeQuizzes = await _quizRepo.GetQuizzesForStudentAsync(studentId);
            var attempts = await _attemptRepo.GetAttemptsByStudentAsync(studentId);

            // Bổ sung các bài thi đã bị xóa nhưng sinh viên đã có lịch sử làm bài
            var attemptedQuizIds = attempts.Select(a => a.QuizId).Distinct().ToList();
            var activeQuizIds = activeQuizzes.Select(q => q.Id).ToList();
            var missingQuizIds = attemptedQuizIds.Except(activeQuizIds).ToList();

            var allQuizzesList = activeQuizzes.ToList();

            if (missingQuizIds.Any())
            {
                var deletedQuizzes = await _quizRepo.GetQuizzesByIdsIncludeDeletedAsync(missingQuizIds);
                allQuizzesList.AddRange(deletedQuizzes);
            }

            var result = new List<StudentQuizDto>();

            foreach (var q in allQuizzesList)
            {
                
                var studentAttemptsCount = attempts.Count(a => a.QuizId == q.Id);
                var isCompleted = attempts.Any(a => a.QuizId == q.Id && a.Status == "Graded");
                var inProgress = attempts.Any(a => a.QuizId == q.Id && a.Status == "InProgress");
                
                string status = "Chưa làm";
                if (inProgress)
                    status = "Đang làm";
                else if (q.EndTime.HasValue && DateTime.UtcNow > q.EndTime.Value)
                    status = "Hết hạn";
                else if (studentAttemptsCount >= q.MaxAttempts && q.MaxAttempts > 0)
                    status = "Hết lượt";
                else if (isCompleted)
                    status = "Còn Lượt";
               
                result.Add(new StudentQuizDto
                {
                    QuizId = q.Id,
                    Title = q.Title,
                    SubjectId = q.SubjectId,
                    SubjectName = q.Subject?.Name ?? "N/A",
                    CreatedAt = q.CreatedAt,
                    TimeLimitMinutes = q.TimeLimitMinutes,
                    MaxAttempts = q.MaxAttempts,
                    AttemptsCount = studentAttemptsCount,
                    StartTime = q.StartTime,
                    EndTime = q.EndTime,
                    Status = status,
                    HasPassword = !string.IsNullOrEmpty(q.AccessCode),
                    LatestAttemptId = attempts.Where(a => a.QuizId == q.Id).OrderByDescending(a => a.StartTime).FirstOrDefault()?.Id
                });
            }

            return result.OrderByDescending(q => q.CreatedAt).ToList();
        }

        public async Task<(bool Success, string Message)> CheckQuizAccessCodeAsync(int studentId, int quizId, string? accessCode)
        {
            var quiz = await _quizRepo.GetByIdAsync(quizId);
            if (quiz == null) return (false, "Bài thi không tồn tại.");

            // Nếu sinh viên đang có bài làm dở, cho phép quay lại mà không cần nhập lại mật khẩu
            // (đồng nhất với logic trong StartQuizAsync).
            var previousAttempts = await _attemptRepo.GetAttemptsByStudentAsync(studentId, quizId);
            if (previousAttempts.Any(a => a.Status == "InProgress"))
                return (true, string.Empty);

            if (!string.IsNullOrEmpty(quiz.AccessCode) && quiz.AccessCode != accessCode)
                return (false, "Mật khẩu bài thi không chính xác.");
            return (true, string.Empty);
        }

        public async Task<TakeQuizDto?> GetInProgressAttemptSummaryAsync(int studentId, int attemptId)
        {
            var attempt = await _attemptRepo.GetAttemptWithAnswersAsync(attemptId);
            if (attempt == null || attempt.StudentId != studentId || attempt.Status != "InProgress")
                return null;

            return new TakeQuizDto
            {
                AttemptId = attempt.Id,
                Title = attempt.Quiz?.Title ?? "Bài thi",
                StartTime = attempt.StartTime,
                TimeLimitMinutes = attempt.Quiz?.TimeLimitMinutes ?? 0,
                Questions = attempt.Answers.Select(a => new TakeQuizQuestionDto
                {
                    QuestionBankId = a.QuestionBankId,
                    SelectedAnswer = a.SelectedAnswer
                }).ToList()
            };
        }

        public async Task<QuizDetailDto> GetQuizDetailAsync(int quizId, int studentId)
        {
            var quiz = await _quizRepo.GetByIdAsync(quizId);
            if (quiz == null)
                throw new Exception("Bài thi không tồn tại.");

            var previousAttempts = (await _attemptRepo.GetAttemptsByStudentAsync(studentId, quizId)).ToList();
            
            bool showAnswers = quiz.ShowScoreAfterSubmit;
            if (showAnswers && quiz.ScoreDisplayTiming == "AfterEndTime")
            {
                if (quiz.EndTime.HasValue && DateTime.UtcNow < quiz.EndTime.Value)
                {
                    showAnswers = false;
                }
            }

            decimal? finalScore = null;
            if (showAnswers)
            {
                var gradedAttempts = previousAttempts.Where(a => a.Status == "Graded").ToList();
                if (gradedAttempts.Any())
                {
                    if (quiz.GradingMethod == "Highest")
                    {
                        finalScore = gradedAttempts.Max(a => a.Score);
                    }
                    else if (quiz.GradingMethod == "Average")
                    {
                        finalScore = gradedAttempts.Average(a => a.Score);
                    }
                    else // Latest
                    {
                        var latest = gradedAttempts.OrderByDescending(a => a.EndTime ?? a.StartTime).First();
                        finalScore = latest.Score;
                    }
                }
            }

            return new QuizDetailDto
            {
                Id = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                TimeLimitMinutes = quiz.TimeLimitMinutes,
                MaxAttempts = quiz.MaxAttempts,
                StartTime = quiz.StartTime,
                EndTime = quiz.EndTime,
                NumVariants = quiz.NumVariants,
                GradingMethod = quiz.GradingMethod,
                ShowScoreAfterSubmit = showAnswers,
                ScoreDisplayTiming = quiz.ScoreDisplayTiming,
                HasPassword = !string.IsNullOrEmpty(quiz.AccessCode),
                FinalScore = finalScore,
                AttemptsDoneByCurrentUser = previousAttempts.Count,
                Attempts = previousAttempts.Select(a => new QuizAttemptSummaryDto
                {
                    AttemptId = a.Id,
                    StudentName = "", // Not needed for student view
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    Score = a.Score,
                    Status = a.Status
                }).OrderBy(a => a.StartTime).ToList()
            };
        }

        public async Task<List<QuizSummaryDto>> GetQuizzesBySubjectAsync(int subjectId)
        {
            var quizzes = await _quizRepo.GetBySubjectOrLecturerAsync(subjectId, null);
            return quizzes.Select(q => new QuizSummaryDto
            {
                Id = q.Id,
                Title = q.Title,
                SubjectId = q.SubjectId,
                TimeLimitMinutes = q.TimeLimitMinutes,
                NumVariants = q.NumVariants,
                CreatedAt = q.CreatedAt
            }).ToList();
        }

        public async Task<TakeQuizDto> StartQuizAsync(int studentId, int quizId, string? accessCode, bool createNew = false)
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

            var previousAttempts = await _attemptRepo.GetAttemptsByStudentAsync(studentId, quizId);
            
            var inProgressAttempt = previousAttempts.FirstOrDefault(a => a.Status == "InProgress");
            
            // Nếu đã có bài đang làm dở, cho phép quay lại mà không cần check mật khẩu lại
            if (inProgressAttempt == null)
            {
                // Chỉ check mật khẩu khi bắt đầu lượt MỚI
                if (!string.IsNullOrEmpty(quiz.AccessCode) && quiz.AccessCode != accessCode)
                    throw new Exception("Mật khẩu bài thi không chính xác.");
            }

            if (inProgressAttempt != null)
            {
                var attemptWithAnswers = await _attemptRepo.GetAttemptWithAnswersAsync(inProgressAttempt.Id);
                if (attemptWithAnswers != null)
                {
                    return new TakeQuizDto
                    {
                        AttemptId = attemptWithAnswers.Id,
                        Title = quiz.Title,
                        StartTime = attemptWithAnswers.StartTime,
                        TimeLimitMinutes = quiz.TimeLimitMinutes,
                        Questions = attemptWithAnswers.Answers.Select(a => new TakeQuizQuestionDto
                        {
                            QuestionBankId = a.QuestionBankId,
                            QuestionText = a.QuestionBank?.Content ?? "N/A",
                            Type = a.QuestionBank?.QuestionType ?? "MultipleChoice",
                            Options = GetOptionsList(a.QuestionBank),
                            SelectedAnswer = a.SelectedAnswer
                        }).ToList()
                    };
                }
            }

            if (!createNew)
                throw new Exception("NO_IN_PROGRESS");

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
            if (qb == null) 
                return options;
                
            if (qb.QuestionType == "TrueFalse")
            {
                return new List<string> { "True", "False" };
            }

            if (string.IsNullOrEmpty(qb.OptionsJson))
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

        public async Task SaveQuizProgressAsync(int studentId, SubmitQuizDto dto)
        {
            var attempt = await _attemptRepo.GetAttemptWithAnswersAsync(dto.AttemptId);
            
            if (attempt == null || attempt.StudentId != studentId)
                throw new Exception("Lần làm bài không tồn tại hoặc bạn không có quyền truy cập.");

            if (attempt.Status != "InProgress")
                throw new Exception("Bài thi này đã được nộp, không thể lưu.");

            foreach (var answerRecord in attempt.Answers)
            {
                var submittedAnswer = dto.Answers.FirstOrDefault(a => a.QuestionBankId == answerRecord.QuestionBankId);
                
                if (submittedAnswer != null && !string.IsNullOrEmpty(submittedAnswer.SelectedAnswer))
                {
                    answerRecord.SelectedAnswer = submittedAnswer.SelectedAnswer;
                }
                else
                {
                    answerRecord.SelectedAnswer = null;
                }
            }

            await _attemptRepo.UpdateAttemptAsync(attempt);
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
                // Nếu dto có truyền đáp án lên thì cập nhật lại, nếu không thì dùng đáp án đã lưu
                var submittedAnswer = dto.Answers?.FirstOrDefault(a => a.QuestionBankId == answerRecord.QuestionBankId);
                
                if (submittedAnswer != null)
                {
                    answerRecord.SelectedAnswer = string.IsNullOrEmpty(submittedAnswer.SelectedAnswer) ? null : submittedAnswer.SelectedAnswer;
                }

                // Chấm điểm dựa trên answerRecord.SelectedAnswer hiện tại
                if (!string.IsNullOrEmpty(answerRecord.SelectedAnswer))
                {
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
            var quiz = await _quizRepo.GetByIdIncludeDeletedAsync(attempt.QuizId);
            bool showAnswers = quiz?.ShowScoreAfterSubmit ?? true;
            
            if (showAnswers && quiz?.ScoreDisplayTiming == "AfterEndTime")
            {
                if (quiz.EndTime.HasValue && DateTime.UtcNow < quiz.EndTime.Value)
                {
                    showAnswers = false;
                }
            }

            var resultDto = new QuizResultDto
            {
                AttemptId = attempt.Id,
                ShowScoreAfterSubmit = showAnswers,
                Score = showAnswers ? attempt.Score : 0,
                CorrectCount = showAnswers ? attempt.CorrectCount : 0,
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
                    IsCorrect = showAnswers ? (bool?)a.IsCorrect : null,
                    Explanation = null
                    
                }).ToList()
            };

            return resultDto;
        }
    }
}
