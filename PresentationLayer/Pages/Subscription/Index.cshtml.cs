using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Subscription
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ISubscriptionPlanService _planService;

        public IndexModel(ISubscriptionService subscriptionService, ISubscriptionPlanService planService)
        {
            _subscriptionService = subscriptionService;
            _planService = planService;
        }

        public SubscriptionInfoDto Info { get; set; } = new();
        public List<SubscriptionPlanDto> Plans { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = GetUserId();
            if (userId > 0)
            {
                Info = await _subscriptionService.GetSubscriptionInfoAsync(userId);
            }

            var activePlans = await _planService.GetAllAsync();
            Plans = activePlans.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToList();
        }

        public IActionResult OnPostUpgrade(string plan)
        {
            var userId = GetUserId();
            if (userId <= 0)
                return RedirectToPage("/Auth/Login");

            // Redirect to the Payment Create page which will forward to VNPay
            return RedirectToPage("/Payment/Create", new { plan = plan });
        }

        private int GetUserId()
        {
            var val = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value
                   ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : 0;
        }
    }
}
