using System.Threading.Tasks;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;

namespace DataAccessLayer.Repositories
{
    /// <summary>Repository nhật ký tiêu thụ token AI (TokenUsageLog).</summary>
    public class TokenUsageLogRepository : ITokenUsageLogRepository
    {
        private readonly ApplicationDbContext _context;

        public TokenUsageLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(TokenUsageLog log)
        {
            _context.TokenUsageLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
