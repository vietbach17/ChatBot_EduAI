using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.IRepositories;

namespace DataAccessLayer.Repositories
{
    /// <summary>
    /// Repository truy vấn danh mục Gói đăng ký từ PostgreSQL.
    /// </summary>
    public class SubscriptionPlanRepository : ISubscriptionPlanRepository
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionPlanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SubscriptionPlan>> GetAllAsync()
            => await _context.SubscriptionPlans.OrderBy(p => p.SortOrder).ToListAsync();

        public async Task<SubscriptionPlan?> GetByIdAsync(int id)
            => await _context.SubscriptionPlans.FindAsync(id);

        public async Task<SubscriptionPlan?> GetByNameAsync(string name)
            => await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Name == name);

        public async Task AddAsync(SubscriptionPlan plan)
        {
            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SubscriptionPlan plan)
        {
            _context.SubscriptionPlans.Update(plan);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(id);
            if (plan != null)
            {
                _context.SubscriptionPlans.Remove(plan);
                await _context.SaveChangesAsync();
            }
        }
    }
}
