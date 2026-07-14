using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Giao diện dịch vụ Xác thực: đăng nhập, đăng ký, quên mật khẩu, đổi mật khẩu.
    /// </summary>
    public interface IAuthService
    {
        Task<UserDto?> AuthenticateAsync(LoginDto loginDto);
        Task<(bool Success, string Error)> RegisterAsync(RegisterDto registerDto);
        
        // FORGOT PASSWORD
        Task<(bool Success, string Message)> GenerateAndSendOtpAsync(string email);
        Task<(bool Success, string Message)> VerifyOtpAsync(string email, string otp);
        Task<(bool Success, string Message)> ResetPasswordAsync(string email, string otp, string newPassword);
    }
}
