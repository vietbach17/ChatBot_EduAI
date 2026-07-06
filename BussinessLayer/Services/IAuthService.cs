using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.Services
{
    public interface IAuthService
    {
        Task<UserDto?> AuthenticateAsync(LoginDto loginDto);
        Task<(bool Success, string Error)> RegisterAsync(RegisterDto registerDto);
    }
}
