using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PresentationLayer.SignalR
{
    [Authorize]
    /// <summary>
    /// SignalR Hub xu ly ket noi realtime cho chuc nang chat streaming. Client ket noi va goi phuong thuc SendStreamingMessage de nhan phan hoi AI theo tung chunk (ReceiveChunk), hoan tat (StreamComplete) hoac loi (StreamError).
    /// </summary>
    public class SignalRHub : Hub
    {
        private readonly IChatService _chatService;

        public SignalRHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Client goi method nay de gui tin nhan va nhan streaming response.
        /// Client lang nghe: ReceiveChunk, StreamComplete, StreamError
        /// </summary>
        public async Task SendStreamingMessage(ChatRequestDto request)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                await Clients.Caller.SendAsync("StreamError", "Phien dang nhap het han, vui long dang nhap lai.", false);
                return;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                await Clients.Caller.SendAsync("StreamError", "Tin nhan khong hop le.", false);
                return;
            }

            using var cts = new CancellationTokenSource();
            // Huy stream neu client disconnect
            Context.ConnectionAborted.Register(() => cts.Cancel());

            ChatResponseDto result;
            try
            {
                result = await _chatService.ProcessStreamingChatMessageAsync(
                    userId,
                    request,
                    async chunk =>
                    {
                        // Gui tung chunk ve client
                        if (!cts.Token.IsCancellationRequested)
                            await Clients.Caller.SendAsync("ReceiveChunk", chunk, cts.Token);
                    },
                    cts.Token);
            }
            catch (OperationCanceledException)
            {
                return; // Client da disconnect
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("StreamError", "Loi he thong: " + ex.Message, false);
                return;
            }

            if (!result.Success)
            {
                await Clients.Caller.SendAsync("StreamError", result.Message ?? "Loi khong xac dinh.", result.OutOfQuota);
                return;
            }

            // Gui thong tin hoan tat: sessionId, title, remaining, citations
            await Clients.Caller.SendAsync("StreamComplete", new
            {
                sessionId = result.SessionId,
                sessionTitle = result.SessionTitle,
                remaining = result.Remaining,
                citations = result.Citations,
                reply = result.Reply
            });
        }

        private int GetUserId()
        {
            var userIdStr = Context.User?.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdStr, out var parsedId)) return parsedId;
            return 0;
        }
    }
}
