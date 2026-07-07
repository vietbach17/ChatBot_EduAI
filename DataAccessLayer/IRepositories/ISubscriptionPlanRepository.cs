using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    public interface ISubscriptionPlanRepository
    {
        Task<IEnumerable<SubscriptionPlan>> GetAllAsync();
        Task<SubscriptionPlan?> GetByIdAsync(int id);
        Task<SubscriptionPlan?> GetByNameAsync(string name);
        Task AddAsync(SubscriptionPlan plan);
        Task UpdateAsync(SubscriptionPlan plan);
        Task DeleteAsync(int id);
    }
}
