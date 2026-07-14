using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;

namespace DataAccessLayer.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync(bool includeDeleted = false)
        {
            var query = _context.Users.AsQueryable();
            if (!includeDeleted)
            {
                query = query.Where(u => !u.IsDeleted);
            }
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<User>> GetLecturersAsync()
        {
            return await _context.Users.Where(u => u.Role == "Lecturer" && !u.IsDeleted).ToListAsync();
        }

        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
