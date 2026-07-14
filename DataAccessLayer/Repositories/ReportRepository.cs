using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    /// <summary>
    /// Repository truy vấn Báo cáo / Khiếu nại từ PostgreSQL.
    /// </summary>
    public class ReportRepository : IReportRepository
    {
        private readonly ApplicationDbContext _context;

        public ReportRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Report report)
        {
            await _context.Reports.AddAsync(report);
            await _context.SaveChangesAsync();
        }

        public async Task<Report?> GetByIdAsync(int id)
            => await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.HandledByAdmin)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<IEnumerable<Report>> GetByReporterIdAsync(int reporterId)
            => await _context.Reports
                .Include(r => r.HandledByAdmin)
                .Where(r => r.ReporterId == reporterId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<Report>> GetAllAsync()
            => await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.HandledByAdmin)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task UpdateAsync(Report report)
        {
            _context.Reports.Update(report);
            await _context.SaveChangesAsync();
        }
    }
}
