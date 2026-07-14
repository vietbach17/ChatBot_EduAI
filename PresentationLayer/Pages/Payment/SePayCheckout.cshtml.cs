using System.Threading.Tasks;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Payment
{
    /// <summary>
    /// PageModel trang Thanh toan SePay. Hien thi thong tin tai khoan ngan hang va ma QR de nguoi dung chuyen khoan.
    /// </summary>
    public class SePayCheckoutModel : PageModel
    {
        private readonly IPaymentService _paymentService;

        public SePayCheckoutModel(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        public string BankId { get; set; } = string.Empty;
        public string AccountNo { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string QrCodeUrl { get; set; } = string.Empty;
        public int TransactionId { get; set; }

        public IActionResult OnGet(string bankId, string accountNo, string amount, string content, string returnUrl, int transactionId)
        {
            if (string.IsNullOrEmpty(bankId) || string.IsNullOrEmpty(accountNo) || string.IsNullOrEmpty(amount) || string.IsNullOrEmpty(content))
            {
                return RedirectToPage("/Subscription/Index");
            }

            BankId = bankId;
            AccountNo = accountNo;
            Amount = amount;
            Content = content;
            ReturnUrl = returnUrl;
            TransactionId = transactionId;

            // Generate VietQR URL
            QrCodeUrl = $"https://img.vietqr.io/image/{BankId}-{AccountNo}-compact2.png?amount={Amount}&addInfo={Content}&accountName=CHATEDU";

            return Page();
        }

        /// <summary>Kiểm tra trạng thái giao dịch hiện tại (dùng cho nút "Tôi đã chuyển khoản xong").</summary>
        public async Task<IActionResult> OnGetCheckStatusAsync(int transactionId)
        {
            var transaction = await _paymentService.GetTransactionByIdAsync(transactionId);
            if (transaction == null)
                return new JsonResult(new { success = false, status = "NotFound" });

            return new JsonResult(new { success = true, status = transaction.Status });
        }
    }
}
