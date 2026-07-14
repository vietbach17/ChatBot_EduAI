using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    /// <summary>
    /// Giao diện Repository truy vấn Gói mua thêm (AddonPackage).
    /// </summary>
    public interface IAddonPackageRepository
    {
        Task<IEnumerable<AddonPackage>> GetAllActiveAsync();
        Task<AddonPackage?> GetByIdAsync(int id);
        Task<AddonPackage> AddAsync(AddonPackage package);
        Task UpdateAsync(AddonPackage package);
        Task DeleteAsync(int id);
    }
}
