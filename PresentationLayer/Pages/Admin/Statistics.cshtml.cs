using System;
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
    /// PageModel trang Thống kê của Admin: token AI tiêu thụ theo tháng/năm,
    /// doanh thu subscription theo tháng/năm (biểu đồ cột + line), và bảng chi tiết theo từng user.
    /// </summary>
    public class StatisticsModel : PageModel
    {
        private readonly IStatisticsService _statisticsService;

        public StatisticsModel(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        public AdminStatisticsDto Stats { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? Year { get; set; }

        public async Task OnGetAsync()
        {
            var year = Year ?? DateTime.UtcNow.Year;
            Stats = await _statisticsService.GetAdminStatisticsAsync(year);
        }
    }
}
