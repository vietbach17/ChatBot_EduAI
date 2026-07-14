using DataAccessLayer.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLayer.IRepositories
{
    /// <summary>
    /// Giao diện Repository truy vấn Báo cáo / Khiếu nại.
    /// </summary>
    public interface IReportRepository
    {
        Task AddAsync(Report report);
        Task<Report?> GetByIdAsync(int id);
        Task<IEnumerable<Report>> GetByReporterIdAsync(int reporterId);
        Task<IEnumerable<Report>> GetAllAsync();
        Task UpdateAsync(Report report);
    }
}
