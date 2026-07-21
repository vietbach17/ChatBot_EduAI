using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using DataAccessLayer.IRepositories;

using BussinessLayer.IServices;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Dịch vụ Quản lý Người dùng. Cung cấp các thao tác: lấy danh sách, tìm kiếm, cập nhật thông tin, đổi vai trò, và xóa mềm (Soft Delete).
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync(bool includeDeleted = false)
        {
            var users = await _userRepository.GetAllUsersAsync(includeDeleted);
            return users.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role,
                Email = u.Email,
                IsDeleted = u.IsDeleted
            }).ToList();
        }

        public async Task<IEnumerable<UserDto>> GetLecturersAsync()
        {
            var users = await _userRepository.GetLecturersAsync();
            return users.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role,
                Email = u.Email,
                IsDeleted = u.IsDeleted
            }).ToList();
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null) return null;
            return new UserDto { 
                Id = user.Id, 
                Username = user.Username, 
                Role = user.Role, 
                Email = user.Email, 
                IsDeleted = user.IsDeleted,
                CustomChunkMaxWords = user.CustomChunkMaxWords,
                CustomChunkOverlapWords = user.CustomChunkOverlapWords
            };
        }

        public async Task<bool> CreateUserAsync(string username, string password, string role, string? email)
        {
            var existing = await _userRepository.GetUserByUsernameAsync(username);
            if (existing != null) return false;

            var user = new User
            {
                Username = username,
                PasswordHash = password, // In real app, hash password!
                Role = role,
                Email = email
            };
            await _userRepository.AddUserAsync(user);
            return true;
        }

        public async Task<bool> UpdateUserAsync(int id, string username, string role, string? email, string? password)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null) return false;

            user.Username = username;
            user.Role = role;
            user.Email = email;
            if (!string.IsNullOrEmpty(password))
            {
                user.PasswordHash = password;
            }

            await _userRepository.UpdateUserAsync(user);
            return true;
        }

        public async Task<bool> UpdateUseExtraQuotaAsync(int userId, bool useExtraQuota)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            user.UseExtraQuota = useExtraQuota;
            await _userRepository.UpdateUserAsync(user);
            return true;
        }

        public async Task<bool> UpdateChunkSettingsAsync(int userId, int? maxWords)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            user.CustomChunkMaxWords = maxWords;
            user.CustomChunkOverlapWords = null; // Always null out overlap words so it uses default
            await _userRepository.UpdateUserAsync(user);
            return true;
        }

        public async Task<bool> SoftDeleteUserAsync(int id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null) return false;

            user.IsDeleted = true;
            await _userRepository.UpdateUserAsync(user);
            return true;
        }

        public async Task<bool> RestoreUserAsync(int id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null) return false;

            user.IsDeleted = false;
            await _userRepository.UpdateUserAsync(user);
            return true;
        }

        public async Task<UserDto?> GetUserByUsernameAsync(string username)
        {
            var user = await _userRepository.GetUserByUsernameAsync(username);
            if (user == null) return null;
            return new UserDto { Id = user.Id, Username = user.Username, Role = user.Role, Email = user.Email, IsDeleted = user.IsDeleted };
        }

        public async Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword)
        {
            var user = await _userRepository.GetUserByUsernameAsync(username);
            if (user == null) return false;

            if (user.PasswordHash != currentPassword) return false;

            user.PasswordHash = newPassword;
            await _userRepository.UpdateUserAsync(user);
            return true;
        }
    }
}
