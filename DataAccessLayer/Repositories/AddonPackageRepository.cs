using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public class AddonPackageRepository : IAddonPackageRepository
    {
        private readonly ApplicationDbContext _context;

        public AddonPackageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AddonPackage>> GetAllActiveAsync()
        {
            return await _context.AddonPackages
                .Where(a => a.IsActive)
                .ToListAsync();
        }

        public async Task<AddonPackage?> GetByIdAsync(int id)
        {
            return await _context.AddonPackages.FindAsync(id);
        }

        public async Task<AddonPackage> AddAsync(AddonPackage package)
        {
            _context.AddonPackages.Add(package);
            await _context.SaveChangesAsync();
            return package;
        }

        public async Task UpdateAsync(AddonPackage package)
        {
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var package = await _context.AddonPackages.FindAsync(id);
            if (package != null)
            {
                _context.AddonPackages.Remove(package);
                await _context.SaveChangesAsync();
            }
        }
    }
}
