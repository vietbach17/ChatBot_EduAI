using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer.Entities;

namespace BussinessLayer.IServices
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync(bool includeDeleted = false);
        Task<IEnumerable<UserDto>> GetLecturersAsync();
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<UserDto?> GetUserByUsernameAsync(string username);
        Task<bool> CreateUserAsync(string username, string password, string role, string? email);
        Task<bool> UpdateUserAsync(int id, string username, string role, string? email, string? password);
        Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword);
        Task<bool> SoftDeleteUserAsync(int id);
        Task<bool> RestoreUserAsync(int id);
    }
}
