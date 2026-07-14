using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    /// <summary>
    /// PageModel trang Quản lý Báo cáo (Admin). Xem toàn bộ báo cáo từ Sinh viên / Giảng viên,
    /// lọc theo vai trò & trạng thái, phản hồi và cập nhật tiến trình xử lý.
    /// </summary>
    public class ReportsModel : PageModel
    {
        private readonly IReportService _reportService;

        public ReportsModel(IReportService reportService)
        {
            _reportService = reportService;
        }

        public List<ReportDto> Reports { get; set; } = new();
        public Dictionary<string, int> StatusCounts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? FilterRole { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            ViewData["ActiveMenu"] = "AdminReports";
            StatusCounts = await _reportService.GetStatusCountsAsync();
            Reports = (await _reportService.GetAllReportsAsync(FilterRole, FilterStatus)).ToList();
        }

        public async Task<IActionResult> OnPostRespondAsync(int reportId, string status, string? adminResponse)
        {
            var adminId = GetAdminId();
            if (adminId == 0) return RedirectToPage("/Auth/Login");

            var ok = await _reportService.RespondAsync(new ReportRespondDto
            {
                ReportId = reportId,
                AdminId = adminId,
                Status = status,
                AdminResponse = adminResponse
            });

            StatusMessage = ok
                ? "Đã cập nhật và phản hồi báo cáo thành công."
                : "Không thể cập nhật báo cáo (trạng thái không hợp lệ hoặc báo cáo không tồn tại).";

            return RedirectToPage(new { FilterRole, FilterStatus });
        }

        private int GetAdminId()
        {
            var val = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value
                   ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : 0;
        }
    }
}
