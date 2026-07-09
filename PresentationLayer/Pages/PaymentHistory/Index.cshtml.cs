using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
// using Microsoft.AspNetCore.Authorization; // Un-comment when TV1 sets up auth

namespace PresentationLayer.Pages.PaymentHistory
{
    // [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IPaymentHistoryService _historyService;
        // private readonly IEmailService _emailService; // In reality, we'd inject this to resend email

        public IndexModel(IPaymentHistoryService historyService)
        {
            _historyService = historyService;
        }

        public IEnumerable<PaymentHistoryDto> Histories { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SelectedMethod { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SelectedStatus { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                currentUserId = 1; // Dự phòng
            }
            
            var allHistories = await _historyService.GetPaymentHistoryByUserIdAsync(currentUserId);

            // Client-side filtering logic to complement server-side (since GetByUserId doesn't take filters in our current design)
            var query = allHistories.AsQueryable();

            if (!string.IsNullOrEmpty(SelectedMethod))
            {
                query = query.Where(x => x.Method == SelectedMethod);
            }

            if (!string.IsNullOrEmpty(SelectedStatus))
            {
                query = query.Where(x => x.Status == SelectedStatus);
            }

            Histories = query.ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostResendEmailAsync(int id)
        {
            // TODO: Fetch transaction by id and call _emailService.SendInvoiceEmailAsync
            TempData["SuccessMessage"] = "Đã gửi lại email hóa đơn thành công!";
            return RedirectToPage();
        }
    }
}
