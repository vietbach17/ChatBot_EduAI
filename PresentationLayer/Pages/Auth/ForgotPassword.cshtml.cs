using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Auth
{
    /// <summary>
    /// PageModel trang Quen Mat khau. Xu ly gui OTP den email nguoi dung va cho phep dat lai mat khau sau khi xac thuc OTP.
    /// </summary>
    public class ForgotPasswordModel : PageModel
    {
        private readonly IAuthService _authService;

        public ForgotPasswordModel(IAuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public int Step { get; set; } = 1;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Otp { get; set; } = string.Empty;

        [BindProperty]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        public void OnGet()
        {
            Step = 1;
        }

        public async Task<IActionResult> OnPostSendOtpAsync()
        {
            if (string.IsNullOrEmpty(Email))
            {
                ModelState.AddModelError("Email", "Vui lòng nhập email.");
                Step = 1;
                return Page();
            }

            var result = await _authService.GenerateAndSendOtpAsync(Email);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                Step = 2; // Chuyển sang bước nhập OTP
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Message);
                Step = 1;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostVerifyOtpAsync()
        {
            if (string.IsNullOrEmpty(Otp))
            {
                ModelState.AddModelError("Otp", "Vui lòng nhập mã OTP.");
                Step = 2;
                return Page();
            }

            var result = await _authService.VerifyOtpAsync(Email, Otp);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Mã OTP hợp lệ! Vui lòng đặt mật khẩu mới.";
                Step = 3; // Chuyển sang bước đổi mật khẩu
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Message);
                Step = 2;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostResetPasswordAsync()
        {
            if (string.IsNullOrEmpty(NewPassword))
            {
                ModelState.AddModelError("NewPassword", "Vui lòng nhập mật khẩu mới.");
                Step = 3;
                return Page();
            }

            if (NewPassword != ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp.");
                Step = 3;
                return Page();
            }

            var result = await _authService.ResetPasswordAsync(Email, Otp, NewPassword);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage("/Auth/Login"); // Về Login
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Message);
                Step = 3;
                return Page();
            }
        }
    }
}
