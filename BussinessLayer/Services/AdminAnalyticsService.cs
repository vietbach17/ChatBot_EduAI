using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;

namespace BussinessLayer.Services
{
    public class AdminAnalyticsService : IAdminAnalyticsService
    {
        private readonly ApplicationDbContext _context;

        public AdminAnalyticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TokenStatsDto>> GetTokenUsageStatsAsync()
        {
            return await _context.ChatMessages
                .Where(m => m.TokenCount != null)
                .GroupBy(m => new { Year = m.Timestamp.Year, Month = m.Timestamp.Month })
                .Select(g => new TokenStatsDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalTokens = g.Sum(m => m.TokenCount ?? 0)
                })
                .OrderBy(s => s.Year)
                .ThenBy(s => s.Month)
                .ToListAsync();
        }

        public async Task<IEnumerable<RevenueStatsDto>> GetRevenueStatsAsync()
        {
            return await _context.PaymentTransactions
                .Where(t => t.Status == "Success")
                .GroupBy(t => new { Year = t.CreatedAt.Year, Month = t.CreatedAt.Month })
                .Select(g => new RevenueStatsDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalAmount = g.Sum(t => t.Amount)
                })
                .OrderBy(s => s.Year)
                .ThenBy(s => s.Month)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserAnalyticsDto>> GetUserAnalyticsListAsync()
        {
            return await _context.Users
                .Select(u => new UserAnalyticsDto
                {
                    UserId = u.Id,
                    Username = u.Username,
                    Role = u.Role,
                    SubscriptionPlan = u.SubscriptionPlan,
                    TotalTokensUsed = _context.ChatSessions
                        .Where(s => s.UserId == u.Id)
                        .SelectMany(s => s.Messages)
                        .Where(m => m.TokenCount != null)
                        .Sum(m => m.TokenCount) ?? 0,
                    TotalAmountSpent = _context.PaymentTransactions
                        .Where(t => t.UserId == u.Id && t.Status == "Success")
                        .Sum(t => (decimal?)t.Amount) ?? 0
                })
                .ToListAsync();
        }
    }
}
