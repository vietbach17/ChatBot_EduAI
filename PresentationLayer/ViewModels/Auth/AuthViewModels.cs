using System.ComponentModel.DataAnnotations;

namespace PresentationLayer.ViewModels.Auth
{
    public class LoginViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [MinLength(3, ErrorMessage = "Tên đăng nhập phải có ít nhất 3 ký tự")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        public string Role { get; set; } = "Student";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ProfilePasswordChangeViewModel
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
