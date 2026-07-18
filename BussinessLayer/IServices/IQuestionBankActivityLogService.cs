using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    /// <summary>Giao diện dịch vụ ghi nhật ký hoạt động Ngân hàng câu hỏi (thêm/sửa/xóa/khôi phục câu hỏi).</summary>
    public interface IQuestionBankActivityLogService
    {
        Task<(IEnumerable<QuestionBankActivityLogDto> Logs, int TotalCount)> GetPagedLogsAsync(int page, int pageSize);
        Task LogActivityAsync(int? questionBankId, int userId, string action, string snippet, string? oldContentJson = null);
    }
}
