using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.IServices;
using DataAccessLayer;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace PresentationLayer.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    /// <summary>PageModel trang Lịch sử Ngân hàng câu hỏi (Admin). Hiển thị nhật ký thêm/sửa/xóa câu hỏi của giảng viên.</summary>
    public class QuestionBanksModel : PageModel
    {
        private readonly IQuestionBankActivityLogService _logService;
        private readonly ApplicationDbContext _context;

        public QuestionBanksModel(IQuestionBankActivityLogService logService, ApplicationDbContext context)
        {
            _logService = logService;
            _context = context;
        }

        public IEnumerable<QuestionBankActivityLog> ManualLogs { get; set; } = new List<QuestionBankActivityLog>();
        public IEnumerable<AIGenerationLog> AILogs { get; set; } = new List<AIGenerationLog>();

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

            // Lấy log AI (Lấy toàn bộ hoặc 50 log gần nhất cho Admin)
            AILogs = await _context.AIGenerationLogs
                .Include(a => a.Lecturer)
                .Include(a => a.Subject)
                .OrderByDescending(a => a.CreatedAt)
                .Take(50)
                .ToListAsync();

            return Page();
        }
    }
}
