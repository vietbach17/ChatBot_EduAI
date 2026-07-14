using BussinessLayer.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Giao diện dịch vụ ghi nhật ký hoạt động Bài thi.
    /// </summary>
    public interface IQuizActivityLogService
    {
        Task LogActivityAsync(int subjectId, int? quizId, string quizTitle, int userId, string action);
        Task<IEnumerable<QuizActivityLogDto>> GetLogsBySubjectIdAsync(int subjectId);
    }
}
