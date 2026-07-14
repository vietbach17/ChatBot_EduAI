using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using DataAccessLayer.IRepositories;

using BussinessLayer.IServices;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Dịch vụ Xác thực và Phân quyền. Xử lý đăng nhập, đăng ký, tạo JWT Token, đổi mật khẩu, và quên mật khẩu (gửi OTP qua Email).
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserDto?> AuthenticateAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetUserByUsernameAsync(loginDto.Username);
            
            if (user != null)
            {
                bool isValid = false;

                // Kiểm tra xem mật khẩu đang lưu có phải là BCrypt hash không
                if (user.PasswordHash.StartsWith("$2a$") || user.PasswordHash.StartsWith("$2b$") || user.PasswordHash.StartsWith("$2y$"))
                {
                    isValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
                }
                else
                {
                    // Cơ chế tương thích ngược (Backward Compatibility)
                    // Nếu là tài khoản cũ lưu mật khẩu thường, kiểm tra dạng thường
                    if (user.PasswordHash == loginDto.Password)
                    {
                        isValid = true;
                        // Tự động băm lại mật khẩu và cập nhật ngay vào DB để nâng cấp bảo mật
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(loginDto.Password);
                        await _userRepository.UpdateUserAsync(user);
                    }
                }

                if (isValid)
                {
                    return new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Role = user.Role
                    };
                }
            }

            return null;
        }

        public async Task<(bool Success, string Error)> RegisterAsync(RegisterDto registerDto)
        {
            // Kiểm tra username đã tồn tại chưa
            var existing = await _userRepository.GetUserByUsernameAsync(registerDto.Username);
            if (existing != null)
            {
                return (false, "Tên đăng nhập đã được sử dụng.");
            }

            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Role = "Student"
            };

            await _userRepository.AddUserAsync(user);
            return (true, string.Empty);
        }

        // ==========================================
        // FORGOT PASSWORD
        // ==========================================
        public async Task<(bool Success, string Message)> GenerateAndSendOtpAsync(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                return (false, "Không tìm thấy tài khoản nào với địa chỉ email này.");
            }

            // Sinh mã OTP 6 số ngẫu nhiên
            var random = new System.Random();
            var otp = random.Next(100000, 999999).ToString();

            // Lưu OTP vào DB với hạn 5 phút
            user.ResetOtp = otp;
            user.ResetOtpExpiry = System.DateTime.UtcNow.AddMinutes(5);
            await _userRepository.UpdateUserAsync(user);

            // Gửi email chứa OTP
            var emailService = new EmailService(); // Hoặc inject IEmailService qua DI
            try 
            {
                await emailService.SendPasswordResetOtpAsync(email, otp);
                return (true, "Mã OTP đã được gửi đến email của bạn.");
            }
            catch(System.Exception)
            {
                return (false, "Có lỗi xảy ra khi gửi email OTP. Vui lòng thử lại sau.");
            }
        }

        public async Task<(bool Success, string Message)> VerifyOtpAsync(string email, string otp)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null) return (false, "Tài khoản không tồn tại.");

            if (user.ResetOtp != otp)
                return (false, "Mã OTP không hợp lệ hoặc không chính xác.");

            if (!user.ResetOtpExpiry.HasValue || user.ResetOtpExpiry.Value < System.DateTime.UtcNow)
                return (false, "Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.");

            return (true, "Mã OTP hợp lệ.");
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(string email, string otp, string newPassword)
        {
            // Kiểm tra lại lần cuối để tránh việc gọi trực tiếp hàm bypass bước xác nhận
            var verifyResult = await VerifyOtpAsync(email, otp);
            if (!verifyResult.Success) return verifyResult;

            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null) return (false, "Tài khoản không tồn tại.");

            // Cập nhật mật khẩu mới với BCrypt
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            
            // Xóa mã OTP để không dùng lại được nữa
            user.ResetOtp = null;
            user.ResetOtpExpiry = null;

            await _userRepository.UpdateUserAsync(user);

            return (true, "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.");
        }
    }
}
