using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    /// <summary>
    /// Repository truy vấn và ghi nhật ký hoạt động Bài thi.
    /// </summary>
    public class QuizActivityLogRepository : IQuizActivityLogRepository
    {
        private readonly ApplicationDbContext _context;

        public QuizActivityLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddLogAsync(QuizActivityLog log)
        {
            await _context.QuizActivityLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<QuizActivityLog>> GetLogsBySubjectIdAsync(int subjectId)
        {
            return await _context.QuizActivityLogs
                .Include(l => l.User)
                .Where(l => l.SubjectId == subjectId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
    }
}
