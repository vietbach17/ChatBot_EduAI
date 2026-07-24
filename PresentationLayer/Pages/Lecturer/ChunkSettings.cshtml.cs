using System.Threading.Tasks;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace PresentationLayer.Pages.Lecturer
{
    [Authorize(Roles = "Lecturer")]
    public class ChunkSettingsModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IChunkSettingsService _chunkSettingsService;
        private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;

        public ChunkSettingsModel(
            IUserService userService, 
            IChunkSettingsService chunkSettingsService,
            Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory)
        {
            _userService = userService;
            _chunkSettingsService = chunkSettingsService;
            _scopeFactory = scopeFactory;
        }

        [BindProperty] public int? CustomChunkMaxWords { get; set; }

        public int DefaultMaxWords { get; set; }

        /// <summary>Khoảng số từ mỗi chunk Admin cho phép Giảng viên tự đặt.</summary>
        public int LecturerMinWords { get; set; }
        public int LecturerMaxWords { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    CustomChunkMaxWords = user.CustomChunkMaxWords;
                }
            }

            var policy = _chunkSettingsService.GetPolicy();
            DefaultMaxWords = policy.MaxWords;
            LecturerMinWords = policy.LecturerMinWords;
            LecturerMaxWords = policy.LecturerMaxWords;

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            // Chặn phía server: min/max trong input HTML chỉ là gợi ý, POST trực tiếp vẫn vượt được.
            var validationError = _chunkSettingsService.ValidateLecturerMaxWords(CustomChunkMaxWords);
            if (validationError != null)
            {
                TempData["ErrorMessage"] = validationError;
                return RedirectToPage();
            }

            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var success = await _userService.UpdateChunkSettingsAsync(userId, CustomChunkMaxWords);
                if (success)
                {
                    TempData["SuccessMessage"] = "Đã lưu cấu hình chunk cá nhân!";
                    TempData["PromptUpdateDocs"] = true; // Prompt the user to update existing docs
                }
                else
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi lưu cấu hình.";
                }
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostResetAsync()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var success = await _userService.UpdateChunkSettingsAsync(userId, null);
                if (success)
                {
                    TempData["SuccessMessage"] = "Đã xóa cấu hình riêng. Sẽ sử dụng cấu hình mặc định của Admin.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa cấu hình.";
                }
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAllDocsAsync()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                // Run in background so we don't block the UI
                _ = Task.Run(async () =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                    
                    var docs = await documentService.GetDocumentsByUploaderAsync(userId);
                    foreach (var doc in docs)
                    {
                        try
                        {
                            await documentService.ReprocessDocumentEmbeddingAsync(doc.Id);
                        }
                        catch
                        {
                            // Ignore errors for individual documents
                        }
                    }
                });

                TempData["SuccessMessage"] = "Đã lên lịch cập nhật lại chunk cho tất cả tài liệu. Quá trình này sẽ chạy ngầm và mất vài phút.";
            }
            return RedirectToPage();
        }
    }
}
