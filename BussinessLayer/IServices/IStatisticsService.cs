using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    /// <summary>Dịch vụ tổng hợp số liệu thống kê cho Admin (token AI, doanh thu, chi tiết theo user).</summary>
    public interface IStatisticsService
    {
        /// <param name="year">Năm dùng cho biểu đồ theo tháng.</param>
        Task<AdminStatisticsDto> GetAdminStatisticsAsync(int year);
    }
}
