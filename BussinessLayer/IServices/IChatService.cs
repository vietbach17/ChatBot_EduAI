using System;
using System.Collections.Generic;
using System.Threading;
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

        /// <summary>
        /// Xử lý tin nhắn với streaming — từng chunk text được gửi qua callback <paramref name="onChunk"/>.
        /// Trả về ChatResponseDto sau khi stream hoàn tất (không có Reply, chỉ có metadata).
        /// </summary>
        Task<ChatResponseDto> ProcessStreamingChatMessageAsync(
            int userId,
            ChatRequestDto request,
            Func<string, Task> onChunk,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Phân trang tin nhắn cũ — dùng cho lazy loading khi scroll lên trên.
        /// </summary>
        Task<List<ChatMessageDto>> GetSessionMessagesPagedAsync(int userId, int sessionId, int page, int pageSize = 20);
    }
}

