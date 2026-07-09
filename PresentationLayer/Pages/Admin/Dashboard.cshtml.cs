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
        private readonly ISubscriptionService _subscriptionService;

        public int TotalUsers { get; set; }
        public int TotalSubjects { get; set; }
        public int TotalDocuments { get; set; }
        public decimal EstimatedRevenue { get; set; }

        public DashboardModel(IUserService userService, ISubscriptionService subscriptionService)
        {
            _userService = userService;
            _subscriptionService = subscriptionService;
        }

        public async Task OnGetAsync()
        {
            var users = await _userService.GetAllUsersAsync(false);
            TotalUsers = users.Count();

            // Stubbed for strict Member 5 scope (missing ISubjectService and IDocumentService)
            TotalSubjects = 0;
            TotalDocuments = 0;

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
