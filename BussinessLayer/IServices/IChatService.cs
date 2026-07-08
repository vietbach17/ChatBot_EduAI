using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    public interface IChatService
    {
        Task<ChatResponseDto> ProcessChatMessageAsync(int userId, ChatRequestDto request);
        Task<List<ChatSessionDto>> GetUserSessionsAsync(int userId);
        Task<ChatSessionDto?> CreateSessionAsync(int userId);
        Task<bool> DeleteSessionAsync(int userId, int sessionId);
        Task<bool> ClearSessionAsync(int userId, int sessionId);
    }
}
