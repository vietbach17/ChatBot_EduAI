using System.ComponentModel.DataAnnotations;

namespace BussinessLayer.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        public string Password { get; set; } = string.Empty;
        
        public string? ReturnUrl { get; set; }
    }
}
