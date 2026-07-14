using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.ViewModels.Admin;

namespace PresentationLayer.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class PlansModel : PageModel
    {
        private readonly ISubscriptionPlanService _planService;

        public PlansModel(ISubscriptionPlanService planService)
        {
            _planService = planService;
        }

        public IEnumerable<SubscriptionPlanDto> Plans { get; set; } = new List<SubscriptionPlanDto>();

        // ── Create ──
        [BindProperty] public PlanCreateViewModel CreateModel { get; set; } = new PlanCreateViewModel();

        // ── Update ──
        [BindProperty] public PlanUpdateViewModel UpdateModel { get; set; } = new PlanUpdateViewModel();

        public async Task OnGetAsync()
        {
            Plans = await _planService.GetAllAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var (ok, err) = await _planService.CreateAsync(new SubscriptionPlanDto
            {
                Name                 = CreateModel.Name,
                Description          = CreateModel.Description,
                Price                = CreateModel.Price,
                MonthlyQuestionLimit = CreateModel.Limit,
                SortOrder            = CreateModel.SortOrder,
                IsActive             = CreateModel.IsActive
            });

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Thêm gói thành công!" : err;
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            var (ok, err) = await _planService.UpdateAsync(new SubscriptionPlanDto
            {
                Id                   = UpdateModel.Id,
                Name                 = UpdateModel.Name,
                Description          = UpdateModel.Description,
                Price                = UpdateModel.Price,
                MonthlyQuestionLimit = UpdateModel.Limit,
                SortOrder            = UpdateModel.SortOrder,
                IsActive             = UpdateModel.IsActive
            });

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Cập nhật gói thành công!" : err;
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var (ok, err) = await _planService.DeleteAsync(id);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Đã xóa gói!" : err;
            return RedirectToPage();
        }
    }
}
