using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Dịch vụ ghi nhật ký hoạt động Bài thi. Lưu lại lịch sử các thao tác: Tạo mới, Cập nhật, Xóa bài thi.
    /// </summary>
    public class QuizActivityLogService : IQuizActivityLogService
    {
        private readonly IQuizActivityLogRepository _repository;

        public QuizActivityLogService(IQuizActivityLogRepository repository)
        {
            _repository = repository;
        }

        public async Task LogActivityAsync(int subjectId, int? quizId, string quizTitle, int userId, string action)
        {
            var log = new QuizActivityLog
            {
                SubjectId = subjectId,
                QuizId = quizId,
                QuizTitle = quizTitle,
                UserId = userId,
                Action = action,
                Timestamp = DateTime.UtcNow
            };
            await _repository.AddLogAsync(log);
        }

        public async Task<IEnumerable<QuizActivityLogDto>> GetLogsBySubjectIdAsync(int subjectId)
        {
            var logs = await _repository.GetLogsBySubjectIdAsync(subjectId);
            return logs.Select(l => new QuizActivityLogDto
            {
                Id = l.Id,
                SubjectId = l.SubjectId,
                QuizId = l.QuizId,
                QuizTitle = l.QuizTitle,
                UserId = l.UserId,
                UserName = l.User?.Username ?? "Unknown",
                Action = l.Action,
                Timestamp = l.Timestamp
            });
        }
    }
}
