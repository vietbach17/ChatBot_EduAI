using System.ComponentModel.DataAnnotations;

namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO nhận dữ liệu đăng ký tài khoản mới.
    /// </summary>
    public class RegisterDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [MaxLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự")]
        public string Username { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
