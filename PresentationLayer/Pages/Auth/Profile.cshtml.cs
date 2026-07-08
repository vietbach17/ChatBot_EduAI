using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.ViewModels.Auth;

namespace PresentationLayer.Pages.Auth
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly BussinessLayer.IServices.IUserService _userService;

        public ProfileModel(BussinessLayer.IServices.IUserService userService)
        {
            _userService = userService;
        }

        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        [BindProperty]
        public ProfilePasswordChangeViewModel PasswordChangeModel { get; set; } = new ProfilePasswordChangeViewModel();

        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; } = false;

        public async Task<IActionResult> OnGetAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return RedirectToPage("/Auth/Login");

            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null) return RedirectToPage("/Auth/Login");

            Username = user.Username;
            Email = user.Email ?? "Chưa có email";
            Role = user.Role;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return RedirectToPage("/Auth/Login");

            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null) return RedirectToPage("/Auth/Login");

            // Populate view data again
            Username = user.Username;
            Email = user.Email ?? "Chưa có email";
            Role = user.Role;

            if (string.IsNullOrEmpty(PasswordChangeModel.CurrentPassword) || string.IsNullOrEmpty(PasswordChangeModel.NewPassword) || string.IsNullOrEmpty(PasswordChangeModel.ConfirmPassword))
            {
                Message = "Vui lòng nhập đầy đủ thông tin.";
                IsSuccess = false;
                return Page();
            }

            if (PasswordChangeModel.NewPassword != PasswordChangeModel.ConfirmPassword)
            {
                Message = "Mật khẩu mới và xác nhận mật khẩu không khớp.";
                IsSuccess = false;
                return Page();
            }

            // Change password via service
            var changed = await _userService.ChangePasswordAsync(username, PasswordChangeModel.CurrentPassword, PasswordChangeModel.NewPassword);
            if (!changed)
            {
                Message = "Mật khẩu hiện tại không đúng.";
                IsSuccess = false;
                return Page();
            }

            Message = "Đổi mật khẩu thành công!";
            IsSuccess = true;

            // Clear inputs on success
            PasswordChangeModel = new ProfilePasswordChangeViewModel();

            return Page();
        }
    }
}
