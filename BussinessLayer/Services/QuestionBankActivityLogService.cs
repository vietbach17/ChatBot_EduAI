using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.IServices;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Dịch vụ ghi nhật ký hoạt động Ngân hàng câu hỏi. Lưu lại lịch sử các thao tác
    /// thêm, sửa, xóa, khôi phục câu hỏi kèm nội dung cũ để phục vụ đối chiếu.
    /// </summary>
    public class QuestionBankActivityLogService : IQuestionBankActivityLogService
    {
        private readonly IQuestionBankActivityLogRepository _repository;

        public QuestionBankActivityLogService(IQuestionBankActivityLogRepository repository)
        {
            _repository = repository;
        }

        public async Task<(IEnumerable<QuestionBankActivityLog> Logs, int TotalCount)> GetPagedLogsAsync(int page, int pageSize)
        {
            var logs = await _repository.GetPagedLogsAsync(page, pageSize);
            var count = await _repository.CountLogsAsync();
            return (logs, count);
        }

        public async Task LogActivityAsync(int? questionBankId, int userId, string action, string snippet, string? oldContentJson = null)
        {
            var log = new QuestionBankActivityLog
            {
                QuestionBankId = questionBankId,
                UserId = userId,
                Action = action,
                QuestionSnippet = snippet.Length > 50 ? snippet.Substring(0, 47) + "..." : snippet,
                OldContentJson = oldContentJson
            };
            await _repository.AddLogAsync(log);
        }
    }
}
