using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using DataAccessLayer.IRepositories;

namespace BussinessLayer.Services
{
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
            
            // Trong thực tế cần so sánh hash password, ở đây demo so sánh chuỗi
            if (user != null && user.PasswordHash == loginDto.Password)
            {
                return new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Role = user.Role
                };
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
                PasswordHash = registerDto.Password, // Thực tế nên hash password
                Role = "Student"
            };

            await _userRepository.AddUserAsync(user);
            return (true, string.Empty);
        }
    }
}
