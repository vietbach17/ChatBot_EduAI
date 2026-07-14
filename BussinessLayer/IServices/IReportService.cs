using BussinessLayer.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Giao diện dịch vụ Báo cáo / Khiếu nại.
    /// Sinh viên & Giảng viên gửi báo cáo; Admin tiếp nhận, phản hồi và xử lý.
    /// </summary>
    public interface IReportService
    {
        Task<int> CreateReportAsync(CreateReportDto dto);
        Task<IEnumerable<ReportDto>> GetMyReportsAsync(int reporterId);
        Task<IEnumerable<ReportDto>> GetAllReportsAsync(string? role = null, string? status = null, string? category = null);
        Task<ReportDto?> GetReportByIdAsync(int id);
        Task<bool> RespondAsync(ReportRespondDto dto);

        /// <summary>Đếm số báo cáo theo trạng thái (cho badge trên dashboard admin).</summary>
        Task<Dictionary<string, int>> GetStatusCountsAsync();
    }
}
