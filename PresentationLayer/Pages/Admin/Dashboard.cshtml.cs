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
        private readonly ISubscriptionService _subscriptionService;

        public int TotalUsers { get; set; }
        public int TotalSubjects { get; set; }
        public int TotalDocuments { get; set; }
        public decimal EstimatedRevenue { get; set; }

        public DashboardModel(IUserService userService, ISubjectService subjectService, IDocumentService documentService, ISubscriptionService subscriptionService)
        {
            _userService = userService;
            _subjectService = subjectService;
            _documentService = documentService;
            _subscriptionService = subscriptionService;
        }

        public async Task OnGetAsync()
        {
            var users = await _userService.GetAllUsersAsync(false);
            TotalUsers = users.Count();

            var subjects = await _subjectService.GetAllSubjectsAsync(false);
            TotalSubjects = subjects.Count();

            var documents = await _documentService.GetAllDocumentsAsync();
            TotalDocuments = documents.Count();

            var subscriptions = await _subscriptionService.GetAllSubscriptionsAsync();
            decimal revenue = 0;
            foreach(var sub in subscriptions.Where(s => s.IsActive))
            {
                if(sub.SubscriptionPlan == "Basic") revenue += 50000;
                else if(sub.SubscriptionPlan == "Premium") revenue += 100000;
            }
            EstimatedRevenue = revenue;
        }
    }
}
