using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    public interface IChatRepository
    {
        Task<ChatSession> CreateSessionAsync(int userId, string title);
        Task<ChatSession?> GetSessionByIdAsync(int sessionId);
        Task<List<ChatSession>> GetUserSessionsAsync(int userId);
        Task AddMessageAsync(ChatMessage message);
        Task UpdateSessionTitleAsync(int sessionId, string title);
        Task DeleteSessionAsync(int sessionId);
        Task ClearSessionAsync(int sessionId);
    }
}
