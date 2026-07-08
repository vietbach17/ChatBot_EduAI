using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DataAccessLayer.IRepositories;

namespace DataAccessLayer.Repositories
{
    public class DocumentActivityLogRepository : IDocumentActivityLogRepository
    {
        private readonly ApplicationDbContext _context;

        public DocumentActivityLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddLogAsync(DocumentActivityLog log)
        {
            await _context.DocumentActivityLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<DocumentActivityLog>> GetLogsBySubjectIdAsync(int subjectId)
        {
            return await _context.DocumentActivityLogs
                .Include(l => l.User)
                .Where(l => l.SubjectId == subjectId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
    }
}
