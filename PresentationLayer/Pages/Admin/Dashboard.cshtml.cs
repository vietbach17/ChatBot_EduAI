using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using System.Linq;

namespace PresentationLayer.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly ISubjectService _subjectService;
        private readonly IDocumentService _documentService;
        private readonly IPaymentHistoryService _paymentHistoryService;

        public int TotalUsers { get; set; }
        public int TotalSubjects { get; set; }
        public int TotalDocuments { get; set; }
        public decimal EstimatedRevenue { get; set; }

        public DashboardModel(
            IUserService userService, 
            ISubjectService subjectService, 
            IDocumentService documentService, 
            IPaymentHistoryService paymentHistoryService)
        {
            _userService = userService;
            _subjectService = subjectService;
            _documentService = documentService;
            _paymentHistoryService = paymentHistoryService;
        }

        public async Task OnGetAsync()
        {
            var users = await _userService.GetAllUsersAsync(false);
            TotalUsers = users.Count();

            var subjects = await _subjectService.GetAllSubjectsAsync(false);
            TotalSubjects = subjects.Count();

            var documents = await _documentService.GetAllDocumentsAsync();
            TotalDocuments = documents.Count();

            // Calculate monthly revenue from successful transactions (from main branch implementation)
            var currentMonth = System.DateTime.Now.Month;
            var currentYear = System.DateTime.Now.Year;
            
            var allTransactions = await _paymentHistoryService.GetAllPaymentHistoriesAsync(status: "Success");
            
            EstimatedRevenue = allTransactions
                .Where(t => t.Date.Month == currentMonth && t.Date.Year == currentYear)
                .Sum(t => t.Amount);
        }
    }
}
