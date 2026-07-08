using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.ViewModels.Admin;

namespace PresentationLayer.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class SubscriptionsModel : PageModel
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionsModel(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        public IEnumerable<UserSubscriptionDto> Subscriptions { get; set; } = new List<UserSubscriptionDto>();

        // Bind for set plan modal
        [BindProperty] public SubscriptionUpdateViewModel UpdateModel { get; set; } = new SubscriptionUpdateViewModel();

        public async Task OnGetAsync()
        {
            Subscriptions = await _subscriptionService.GetAllSubscriptionsAsync();
        }

        public async Task<IActionResult> OnPostSetPlanAsync()
        {
            DateTime? expiry = null;
            if (!string.IsNullOrWhiteSpace(UpdateModel.Expiry) &&
                DateTime.TryParse(UpdateModel.Expiry, out var parsedDate))
            {
                expiry = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            }
            else if (UpdateModel.PlanValue != "Free")
            {
                // Mặc định 1 tháng nếu không nhập ngày
                expiry = DateTime.UtcNow.AddMonths(1);
            }

            await _subscriptionService.AdminSetPlanAsync(UpdateModel.UserId, UpdateModel.PlanValue, expiry);
            TempData["SuccessMessage"] = "Cập nhật gói thành công!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostResetQuotaAsync(int userId)
        {
            await _subscriptionService.AdminResetQuotaAsync(userId);
            TempData["SuccessMessage"] = "Đã khôi phục toàn bộ lượt hỏi!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRevokeAsync(int userId)
        {
            await _subscriptionService.AdminRevokePlanAsync(userId);
            TempData["SuccessMessage"] = "Đã thu hồi gói — chuyển về Free!";
            return RedirectToPage();
        }
    }
}
