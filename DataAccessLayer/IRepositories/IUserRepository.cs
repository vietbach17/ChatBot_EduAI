using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    /// <summary>
    /// Giao diện Repository truy vấn Người dùng.
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int id);
        Task<IEnumerable<User>> GetAllUsersAsync(bool includeDeleted = false);
        Task<IEnumerable<User>> GetLecturersAsync();
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
    }
}
