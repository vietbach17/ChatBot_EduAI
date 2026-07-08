using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Chat
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly IChatService _chatService;
        private readonly IGeminiService _geminiService;

        public IndexModel(IDocumentService documentService, IChatService chatService, IGeminiService geminiService)
        {
            _documentService = documentService;
            _chatService = chatService;
            _geminiService = geminiService;
        }

        public List<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
        public List<ChatSessionDto> ChatSessions { get; set; } = new List<ChatSessionDto>();

        public async Task OnGetAsync()
        {
            var userId = GetUserId();
            Documents = (await _documentService.GetAllDocumentsAsync()).ToList();
            // ChatSessions is loaded asynchronously via OnGetSessionsAsync by frontend JS
        }

        public async Task<IActionResult> OnGetSessionsAsync()
        {
            var userId = GetUserId();
            var sessions = await _chatService.GetUserSessionsAsync(userId);
            return new JsonResult(new { success = true, sessions });
        }

        public async Task<IActionResult> OnGetModelsAsync()
        {
            var models = await _geminiService.GetAvailableModelsAsync();
            return new JsonResult(new { success = true, models });
        }

        public async Task<IActionResult> OnPostCreateSessionAsync()
        {
            var userId = GetUserId();
            var session = await _chatService.CreateSessionAsync(userId);
            return new JsonResult(new { success = session != null, session });
        }

        public async Task<IActionResult> OnPostDeleteSessionAsync([FromBody] ChatSessionActionDto request)
        {
            var userId = GetUserId();
            var ok = await _chatService.DeleteSessionAsync(userId, request.SessionId);
            return new JsonResult(new { success = ok });
        }

        public async Task<IActionResult> OnPostClearSessionAsync([FromBody] ChatSessionActionDto request)
        {
            var userId = GetUserId();
            var ok = await _chatService.ClearSessionAsync(userId, request.SessionId);
            return new JsonResult(new { success = ok });
        }

        public async Task<IActionResult> OnPostSendChatMessageAsync([FromBody] ChatRequestDto request)
        {
            var userId = GetUserId();

            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return new JsonResult(new { success = false, message = "Tin nhắn không hợp lệ" });
            }

            var response = await _chatService.ProcessChatMessageAsync(userId, request);

            return new JsonResult(new
            {
                success = response.Success,
                reply = response.Reply,
                message = response.Message,
                remaining = response.Remaining,
                outOfQuota = response.OutOfQuota,
                sessionId = response.SessionId,
                sessionTitle = response.SessionTitle,
                citations = response.Citations
            });
        }

        public async Task<IActionResult> OnGetDocumentTextAsync(int docId)
        {
            var doc = await _documentService.GetDocumentByIdAsync(docId);
            if (doc == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy tài liệu." });
            }

            var text = await _documentService.GetDocumentTextAsync(docId);
            return new JsonResult(new
            {
                success = true,
                title = doc.Title,
                fileType = doc.FileType,
                text = text
            });
        }

        private int GetUserId()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdStr, out var parsedId))
            {
                return parsedId;
            }

            return 0;
        }
    }
}
