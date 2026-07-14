using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Subscription
{
    [Authorize]
    /// <summary>
    /// PageModel trang Goi Hoi vien. Hien thi cac goi dang ky, goi add-on va thong tin goi hien tai cua nguoi dung.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ISubscriptionPlanService _planService;
        private readonly DataAccessLayer.IRepositories.IAddonPackageRepository _addonRepository;

        public IndexModel(ISubscriptionService subscriptionService, ISubscriptionPlanService planService, DataAccessLayer.IRepositories.IAddonPackageRepository addonRepository)
        {
            _subscriptionService = subscriptionService;
            _planService = planService;
            _addonRepository = addonRepository;
        }

        public SubscriptionInfoDto Info { get; set; } = new();
        public List<SubscriptionPlanDto> Plans { get; set; } = new();
        public List<DataAccessLayer.Entities.AddonPackage> Addons { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = GetUserId();
            if (userId > 0)
            {
                Info = await _subscriptionService.GetSubscriptionInfoAsync(userId);
            }

            var activePlans = await _planService.GetAllAsync();
            Plans = activePlans.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToList();

            var activeAddons = await _addonRepository.GetAllActiveAsync();
            Addons = activeAddons.ToList();
        }

        public IActionResult OnPostUpgrade(string plan)
        {
            var userId = GetUserId();
            if (userId <= 0)
                return RedirectToPage("/Auth/Login");

            // Redirect to the Payment Create page which will forward to VNPay
            return RedirectToPage("/Payment/Create", new { plan = plan });
        }

        public IActionResult OnPostBuyAddon(int addonId)
        {
            var userId = GetUserId();
            if (userId <= 0)
                return RedirectToPage("/Auth/Login");

            return RedirectToPage("/Payment/Create", new { addonId = addonId });
        }

        private int GetUserId()
        {
            var val = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value
                   ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : 0;
        }
    }
}
