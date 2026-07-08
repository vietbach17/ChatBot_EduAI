using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;

namespace DataAccessLayer.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly ApplicationDbContext _context;

        public ChatRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ChatSession> CreateSessionAsync(int userId, string title)
        {
            var session = new ChatSession
            {
                UserId = userId,
                Title = title
            };
            
            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<ChatSession?> GetSessionByIdAsync(int sessionId)
        {
            return await _context.ChatSessions
                .Include(s => s.Messages.OrderBy(m => m.Timestamp))
                .FirstOrDefaultAsync(s => s.Id == sessionId);
        }

        public async Task<List<ChatSession>> GetUserSessionsAsync(int userId)
        {
            return await _context.ChatSessions
                .Include(s => s.Messages.OrderBy(m => m.Timestamp))
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync();
        }

        public async Task AddMessageAsync(ChatMessage message)
        {
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSessionTitleAsync(int sessionId, string title)
        {
            var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null) return;

            session.Title = title;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSessionAsync(int sessionId)
        {
            var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null) return;

            _context.ChatSessions.Remove(session);
            await _context.SaveChangesAsync();
        }

        public async Task ClearSessionAsync(int sessionId)
        {
            var session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null) return;

            _context.ChatMessages.RemoveRange(session.Messages);
            session.Title = "Cuộc trò chuyện mới";
            session.CreatedAt = session.CreatedAt;
            await _context.SaveChangesAsync();
        }
    }
}
