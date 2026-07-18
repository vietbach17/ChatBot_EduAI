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
    /// <summary>PageModel trang Chat AI. Quản lý phiên chat, tài liệu, quota và các handler AJAX (gửi tin, tin cũ, model).</summary>
    public class IndexModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly IChatService _chatService;
        private readonly IGeminiService _geminiService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IUserService _userService;

        public IndexModel(IDocumentService documentService, IChatService chatService, IGeminiService geminiService, ISubscriptionService subscriptionService, IUserService userService)
        {
            _documentService = documentService;
            _chatService = chatService;
            _geminiService = geminiService;
            _subscriptionService = subscriptionService;
            _userService = userService;
        }

        public List<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
        public List<ChatSessionDto> ChatSessions { get; set; } = new List<ChatSessionDto>();
        public SubscriptionInfoDto Info { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = GetUserId();
            Documents = (await _documentService.GetAllDocumentsAsync()).ToList();
            if (userId > 0)
            {
                Info = await _subscriptionService.GetSubscriptionInfoAsync(userId);
            }
        }

        /// <summary>Tra ve danh sach sessions cua user</summary>
        public async Task<IActionResult> OnGetSessionsAsync()
        {
            var userId = GetUserId();
            var sessions = await _chatService.GetUserSessionsAsync(userId);
            return new JsonResult(new { success = true, sessions });
        }

        /// <summary>Tra ve thong tin quota hien tai cua user (realtime)</summary>
        public async Task<IActionResult> OnGetQuotaInfoAsync()
        {
            var userId = GetUserId();
            if (userId <= 0)
                return new JsonResult(new { success = false, message = "Chua dang nhap." });
            var info = await _subscriptionService.GetSubscriptionInfoAsync(userId);
            return new JsonResult(new
            {
                success = true,
                plan = info.CurrentPlan,
                monthlyUsed = info.UsedCount,
                monthlyLimit = info.MonthlyLimit,
                shortTermUsed = info.ShortTermUsedCount,
                shortTermLimit = info.ShortTermLimit,
                remaining = info.Remaining,
                extraQuota = info.ExtraQuota,
                useExtraQuota = info.UseExtraQuota
            });
        }


        /// <summary>Bat/tat viec su dung luot hoi du phong cua nguoi dung hien tai</summary>
        public async Task<IActionResult> OnPostToggleExtraQuotaAsync(bool useExtraQuota)
        {
            var userId = GetUserId();
            if (userId <= 0)
                return new JsonResult(new { success = false, message = "Chua dang nhap." });

            var success = await _userService.UpdateUseExtraQuotaAsync(userId, useExtraQuota);
            return new JsonResult(new { success });
        }

        /// <summary>Phan trang tin nhan cu - lazy loading khi scroll len tren</summary>
        public async Task<IActionResult> OnGetSessionMessagesAsync(int sessionId, int page = 0, int pageSize = 20)
        {
            var userId = GetUserId();
            var messages = await _chatService.GetSessionMessagesPagedAsync(userId, sessionId, page, pageSize);
            return new JsonResult(new { success = true, messages });
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

        /// <summary>Fallback non-streaming cho browser khong ho tro SignalR</summary>
        public async Task<IActionResult> OnPostSendChatMessageAsync([FromBody] ChatRequestDto request)
        {
            var userId = GetUserId();

            if (request == null || string.IsNullOrWhiteSpace(request.Message))
                return new JsonResult(new { success = false, message = "Tin nhan khong hop le" });

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
                return new JsonResult(new { success = false, message = "Khong tim thay tai lieu." });

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
            if (int.TryParse(userIdStr, out var parsedId)) return parsedId;
            return 0;
        }
    }
}
