using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Payment
{
    /// <summary>
    /// PageModel trang Thanh toan SePay. Hien thi thong tin tai khoan ngan hang va ma QR de nguoi dung chuyen khoan.
    /// </summary>
    public class SePayCheckoutModel : PageModel
    {
        public string BankId { get; set; } = string.Empty;
        public string AccountNo { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string QrCodeUrl { get; set; } = string.Empty;

        public IActionResult OnGet(string bankId, string accountNo, string amount, string content, string returnUrl)
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

            // Generate VietQR URL
            QrCodeUrl = $"https://img.vietqr.io/image/{BankId}-{AccountNo}-compact2.png?amount={Amount}&addInfo={Content}&accountName=CHATEDU";

            return Page();
        }
    }
}
