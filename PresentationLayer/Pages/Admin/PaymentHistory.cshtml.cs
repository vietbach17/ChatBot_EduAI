using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
// using Microsoft.AspNetCore.Authorization; // Un-comment when TV1 sets up auth

namespace PresentationLayer.Pages.Admin
{
    // [Authorize(Roles = "Admin")]
    /// <summary>
    /// PageModel trang Lich su Thanh toan cua Admin. Hien thi tat ca giao dich, ho tro loc theo trang thai va cap nhat thu cong trang thai thanh toan.
    /// </summary>
    public class PaymentHistoryModel : PageModel
    {
        private readonly IPaymentHistoryService _historyService;
        private readonly ISubscriptionService _subscriptionService;

        public PaymentHistoryModel(IPaymentHistoryService historyService, ISubscriptionService subscriptionService)
        {
            _historyService = historyService;
            _subscriptionService = subscriptionService;
        }

        public IEnumerable<PaymentHistoryDto> Histories { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SelectedMethod { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SelectedStatus { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Histories = await _historyService.GetAllPaymentHistoriesAsync(SearchTerm, SelectedMethod, SelectedStatus);
            return Page();
        }

        public async Task<IActionResult> OnPostMarkAsPaidAsync(int id)
        {
            // Call process payment success logic manually
            // This will reset quota, extend expiry date, and send invoice email
            await _subscriptionService.ProcessPaymentSuccessAsync(id, "MANUAL_APPROVAL", "Xác nhận thủ công bởi Admin", null);
            
            return RedirectToPage(new { SearchTerm, SelectedMethod, SelectedStatus });
        }
    }
}
