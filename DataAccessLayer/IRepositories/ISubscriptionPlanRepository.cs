using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    /// <summary>
    /// Giao diện Repository truy vấn Gói đăng ký.
    /// </summary>
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
