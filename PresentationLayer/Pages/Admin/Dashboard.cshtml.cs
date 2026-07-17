using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using BussinessLayer.DTOs;
using System.Collections.Generic;
using System.Linq;

namespace PresentationLayer.Pages.Admin
{
    /// <summary>
    /// PageModel trang Dashboard của Admin. Hiển thị thống kê tổng quan và biểu đồ dữ liệu.
    /// </summary>
    public class DashboardModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly ISubjectService _subjectService;
        private readonly IDocumentService _documentService;
        private readonly IPaymentHistoryService _paymentHistoryService;
        private readonly IAdminAnalyticsService _analyticsService;

        public int TotalUsers { get; set; }
        public int TotalSubjects { get; set; }
        public int TotalDocuments { get; set; }
        public decimal EstimatedRevenue { get; set; }

        public IEnumerable<TokenStatsDto> TokenStats { get; set; } = new List<TokenStatsDto>();
        public IEnumerable<RevenueStatsDto> RevenueStats { get; set; } = new List<RevenueStatsDto>();
        public IEnumerable<UserAnalyticsDto> UserAnalytics { get; set; } = new List<UserAnalyticsDto>();

        public DashboardModel(
            IUserService userService, 
            ISubjectService subjectService, 
            IDocumentService documentService, 
            IPaymentHistoryService paymentHistoryService,
            IAdminAnalyticsService analyticsService)
        {
            _userService = userService;
            _subjectService = subjectService;
            _documentService = documentService;
            _paymentHistoryService = paymentHistoryService;
            _analyticsService = analyticsService;
        }

        public async Task OnGetAsync()
        {
            var users = await _userService.GetAllUsersAsync(false);
            TotalUsers = users.Count();

            var subjects = await _subjectService.GetAllSubjectsAsync(false);
            TotalSubjects = subjects.Count();

            var documents = await _documentService.GetAllDocumentsAsync();
            TotalDocuments = documents.Count();

            // Calculate monthly revenue from successful transactions
            var currentMonth = System.DateTime.Now.Month;
            var currentYear = System.DateTime.Now.Year;
            
            var allTransactions = await _paymentHistoryService.GetAllPaymentHistoriesAsync(status: "Success");
            
            EstimatedRevenue = allTransactions
                .Where(t => t.Date.Month == currentMonth && t.Date.Year == currentYear)
                .Sum(t => t.Amount);

            // Fetch analytics data
            TokenStats = await _analyticsService.GetTokenUsageStatsAsync();
            RevenueStats = await _analyticsService.GetRevenueStatsAsync();
            UserAnalytics = await _analyticsService.GetUserAnalyticsListAsync();
        }
    }
}
