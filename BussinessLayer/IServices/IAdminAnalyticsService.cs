using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    public interface IAdminAnalyticsService
    {
        Task<IEnumerable<TokenStatsDto>> GetTokenUsageStatsAsync();
        Task<IEnumerable<RevenueStatsDto>> GetRevenueStatsAsync();
        Task<IEnumerable<UserAnalyticsDto>> GetUserAnalyticsListAsync();
        Task<IEnumerable<TokenStatsDto>> GetDailyTokenUsageStatsAsync(int year, int month);
        Task<IEnumerable<RevenueStatsDto>> GetDailyRevenueStatsAsync(int year, int month);
    }
}
