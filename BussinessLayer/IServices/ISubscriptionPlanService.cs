using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    public interface ISubscriptionPlanService
    {
        Task<IEnumerable<SubscriptionPlanDto>> GetAllAsync();
        Task<SubscriptionPlanDto?> GetByIdAsync(int id);
        Task<(bool Success, string Error)> CreateAsync(SubscriptionPlanDto dto);
        Task<(bool Success, string Error)> UpdateAsync(SubscriptionPlanDto dto);
        Task<(bool Success, string Error)> DeleteAsync(int id);
    }
}
