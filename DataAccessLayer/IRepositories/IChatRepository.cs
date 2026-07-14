using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    /// <summary>
    /// Interface repository chat. Dinh nghia cac phuong thuc quan ly ChatSession va ChatMessage trong database.
    /// </summary>
    public interface IChatRepository
    {
        Task<ChatSession> CreateSessionAsync(int userId, string title);
        Task<ChatSession?> GetSessionByIdAsync(int sessionId);
        Task<List<ChatSession>> GetUserSessionsAsync(int userId);
        Task AddMessageAsync(ChatMessage message);
        Task UpdateSessionTitleAsync(int sessionId, string title);
        Task UpdateSessionUpdatedAtAsync(int sessionId);
        Task DeleteSessionAsync(int sessionId);
        Task ClearSessionAsync(int sessionId);

        /// <summary>Lấy tin nhắn phân trang (mới nhất trước), dùng cho lazy loading.</summary>
        Task<List<ChatMessage>> GetMessagesPagedAsync(int sessionId, int page, int pageSize);
    }
}

