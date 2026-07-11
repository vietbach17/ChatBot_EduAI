using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    public interface IAddonPackageRepository
    {
        Task<IEnumerable<AddonPackage>> GetAllActiveAsync();
        Task<AddonPackage?> GetByIdAsync(int id);
        Task<AddonPackage> AddAsync(AddonPackage package);
        Task UpdateAsync(AddonPackage package);
        Task DeleteAsync(int id);
    }
}
