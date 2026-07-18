using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    /// <summary>PageModel trang Lịch sử Ngân hàng câu hỏi (Admin). Hiển thị nhật ký thêm/sửa/xóa câu hỏi của giảng viên.</summary>
    public class QuestionBanksModel : PageModel
    {
        private readonly IQuestionBankActivityLogService _logService;
        private readonly IAIQuizGeneratorService _aiGeneratorService;

        public QuestionBanksModel(IQuestionBankActivityLogService logService, IAIQuizGeneratorService aiGeneratorService)
        {
            _logService = logService;
            _aiGeneratorService = aiGeneratorService;
        }

        public IEnumerable<QuestionBankActivityLogDto> ManualLogs { get; set; } = new List<QuestionBankActivityLogDto>();
        public IEnumerable<AIGenerationLogDto> AILogs { get; set; } = new List<AIGenerationLogDto>();

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }

        public async Task<IActionResult> OnGetAsync(int page = 1)
        {
            ViewData["ActiveMenu"] = "QuestionBanks";
            CurrentPage = page > 0 ? page : 1;

            // Lấy log thủ công (Manual Logs)
            var (logs, totalCount) = await _logService.GetPagedLogsAsync(CurrentPage, PageSize);
            ManualLogs = logs;
            TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);
            if (TotalPages == 0) TotalPages = 1;

            // Lấy log AI (50 log gần nhất cho Admin)
            AILogs = await _aiGeneratorService.GetRecentGenerationLogsAsync(50);

            return Page();
        }
    }
}
