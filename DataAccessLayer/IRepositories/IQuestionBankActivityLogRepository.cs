using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    /// <summary>Giao diện Repository truy vấn và ghi nhật ký hoạt động Ngân hàng câu hỏi.</summary>
    public interface IQuestionBankActivityLogRepository
    {
        Task<IEnumerable<QuestionBankActivityLog>> GetPagedLogsAsync(int page, int pageSize);
        Task<int> CountLogsAsync();
        Task AddLogAsync(QuestionBankActivityLog log);
    }
}
