using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.SignalR;
// using Microsoft.AspNetCore.Authorization; // Un-comment when TV1 sets up auth

namespace PresentationLayer.Pages.PaymentHistory
{
    // [Authorize]
    /// <summary>
    /// PageModel trang Lich su Thanh toan cua nguoi dung. Hien thi danh sach giao dich ca nhan theo thu tu thoi gian moi nhat.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly IPaymentHistoryService _historyService;
        private readonly IHubContext<SignalRHub> _hubContext;

        public IndexModel(IPaymentHistoryService historyService, IHubContext<SignalRHub> hubContext)
        {
            _historyService = historyService;
            _hubContext = hubContext;
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

        public async Task<IActionResult> OnPostCancelPaymentAsync(int id)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                currentUserId = 1; // Dự phòng
            }

            var success = await _historyService.CancelPaymentAsync(id, currentUserId);
            if (success)
            {
                await _hubContext.Clients.All.SendAsync("PaymentStatusUpdated", id, "Cancelled");
                TempData["SuccessMessage"] = "Đã hủy thanh toán thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể hủy thanh toán. Giao dịch có thể không tồn tại hoặc không ở trạng thái Chờ thanh toán.";
            }

            return RedirectToPage();
        }
    }
}
