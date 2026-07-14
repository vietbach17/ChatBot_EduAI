using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Giao diện dịch vụ Quản lý danh mục Gói đăng ký.
    /// </summary>
    public interface ISubscriptionPlanService
    {
        Task<IEnumerable<SubscriptionPlanDto>> GetAllAsync();
        Task<SubscriptionPlanDto?> GetByIdAsync(int id);
        Task<(bool Success, string Error)> CreateAsync(SubscriptionPlanDto dto);
        Task<(bool Success, string Error)> UpdateAsync(SubscriptionPlanDto dto);
        Task<(bool Success, string Error)> DeleteAsync(int id);

        // Gói mua thêm (Addon Package)
        Task<List<AddonPackageDto>> GetActiveAddonsAsync();
        Task<AddonPackageDto?> GetAddonByIdAsync(int id);
    }
}
