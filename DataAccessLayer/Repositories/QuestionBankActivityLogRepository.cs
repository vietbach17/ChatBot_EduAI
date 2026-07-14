using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    /// <summary>Repository truy vấn và ghi nhật ký hoạt động Ngân hàng câu hỏi từ PostgreSQL.</summary>
    public class QuestionBankActivityLogRepository : IQuestionBankActivityLogRepository
    {
        private readonly ApplicationDbContext _context;

        public QuestionBankActivityLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<QuestionBankActivityLog>> GetPagedLogsAsync(int page, int pageSize)
        {
            return await _context.QuestionBankActivityLogs
                .Include(log => log.User)
                .Include(log => log.QuestionBank)
                .OrderByDescending(log => log.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountLogsAsync()
        {
            return await _context.QuestionBankActivityLogs.CountAsync();
        }

        public async Task AddLogAsync(QuestionBankActivityLog log)
        {
            await _context.QuestionBankActivityLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }
    }
}
