using DataAccessLayer.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLayer.IRepositories
{
    /// <summary>
    /// Giao diện Repository truy vấn nhật ký hoạt động Bài thi.
    /// </summary>
    public interface IQuizActivityLogRepository
    {
        Task AddLogAsync(QuizActivityLog log);
        Task<IEnumerable<QuizActivityLog>> GetLogsBySubjectIdAsync(int subjectId);
    }
}
