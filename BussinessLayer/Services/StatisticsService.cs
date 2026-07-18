using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Dịch vụ Thống kê cho Admin: tổng hợp token AI đã tiêu thụ (từ TokenUsageLogs)
    /// và doanh thu subscription/addon (từ PaymentTransactions có Status = Success),
    /// theo tháng / năm và chi tiết theo từng người dùng.
    /// (Dùng trực tiếp ApplicationDbContext vì toàn bộ là truy vấn GroupBy tổng hợp, giống QuotaResetBackgroundService.)
    /// </summary>
    public class StatisticsService : IStatisticsService
    {
        private readonly ApplicationDbContext _context;

        public StatisticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminStatisticsDto> GetAdminStatisticsAsync(int year)
        {
            var result = new AdminStatisticsDto { SelectedYear = year };

            // ── Token theo tháng (năm được chọn) ─────────────────────────────
            var tokenMonthRaw = await _context.TokenUsageLogs
                .Where(t => t.CreatedAt.Year == year)
                .GroupBy(t => t.CreatedAt.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Prompt = g.Sum(x => (long)x.PromptTokens),
                    Output = g.Sum(x => (long)x.OutputTokens),
                    Total = g.Sum(x => (long)x.TotalTokens),
                    Count = g.Count()
                })
                .ToListAsync();

            for (int m = 1; m <= 12; m++)
            {
                var row = tokenMonthRaw.FirstOrDefault(x => x.Month == m);
                result.TokenByMonth.Add(new TokenStatPointDto
                {
                    Label = $"T{m}",
                    PromptTokens = row?.Prompt ?? 0,
                    OutputTokens = row?.Output ?? 0,
                    TotalTokens = row?.Total ?? 0,
                    CallCount = row?.Count ?? 0
                });
            }

            // ── Token theo năm (tất cả các năm có dữ liệu) ───────────────────
            var tokenYearRaw = await _context.TokenUsageLogs
                .GroupBy(t => t.CreatedAt.Year)
                .Select(g => new
                {
                    Year = g.Key,
                    Prompt = g.Sum(x => (long)x.PromptTokens),
                    Output = g.Sum(x => (long)x.OutputTokens),
                    Total = g.Sum(x => (long)x.TotalTokens),
                    Count = g.Count()
                })
                .OrderBy(x => x.Year)
                .ToListAsync();

            result.TokenByYear = tokenYearRaw.Select(x => new TokenStatPointDto
            {
                Label = x.Year.ToString(),
                PromptTokens = x.Prompt,
                OutputTokens = x.Output,
                TotalTokens = x.Total,
                CallCount = x.Count
            }).ToList();

            // ── Doanh thu theo tháng (năm được chọn, chỉ giao dịch thành công) ─
            var payMonthRaw = await _context.PaymentTransactions
                .Where(p => p.Status == "Success" && p.CreatedAt.Year == year)
                .GroupBy(p => p.CreatedAt.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(x => x.Amount), Count = g.Count() })
                .ToListAsync();

            for (int m = 1; m <= 12; m++)
            {
                var row = payMonthRaw.FirstOrDefault(x => x.Month == m);
                result.PaymentByMonth.Add(new PaymentStatPointDto
                {
                    Label = $"T{m}",
                    TotalAmount = row?.Total ?? 0,
                    TransactionCount = row?.Count ?? 0
                });
            }

            // ── Doanh thu theo năm ────────────────────────────────────────────
            var payYearRaw = await _context.PaymentTransactions
                .Where(p => p.Status == "Success")
                .GroupBy(p => p.CreatedAt.Year)
                .Select(g => new { Year = g.Key, Total = g.Sum(x => x.Amount), Count = g.Count() })
                .OrderBy(x => x.Year)
                .ToListAsync();

            result.PaymentByYear = payYearRaw.Select(x => new PaymentStatPointDto
            {
                Label = x.Year.ToString(),
                TotalAmount = x.Total,
                TransactionCount = x.Count
            }).ToList();

            // ── Chi tiết theo từng người dùng ────────────────────────────────
            var tokenByUser = await _context.TokenUsageLogs
                .GroupBy(t => t.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Prompt = g.Sum(x => (long)x.PromptTokens),
                    Output = g.Sum(x => (long)x.OutputTokens),
                    Total = g.Sum(x => (long)x.TotalTokens),
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.UserId);

            var spentByUser = await _context.PaymentTransactions
                .Where(p => p.Status == "Success")
                .GroupBy(p => p.UserId)
                .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Amount), Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId);

            var users = await _context.Users
                .Where(u => !u.IsDeleted)
                .Select(u => new { u.Id, u.Username, u.Email, u.Role, u.SubscriptionPlan })
                .ToListAsync();

            result.Users = users.Select(u =>
            {
                tokenByUser.TryGetValue(u.Id, out var tok);
                spentByUser.TryGetValue(u.Id, out var pay);
                return new UserUsageStatDto
                {
                    UserId = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    SubscriptionPlan = u.SubscriptionPlan,
                    PromptTokens = tok?.Prompt ?? 0,
                    OutputTokens = tok?.Output ?? 0,
                    TotalTokens = tok?.Total ?? 0,
                    ChatCallCount = tok?.Count ?? 0,
                    TotalSpent = pay?.Total ?? 0,
                    PaymentCount = pay?.Count ?? 0
                };
            })
            .OrderByDescending(u => u.TotalTokens)
            .ToList();

            // ── Các năm có dữ liệu (cho dropdown chọn năm) ────────────────────
            var years = tokenYearRaw.Select(x => x.Year)
                .Union(payYearRaw.Select(x => x.Year))
                .Append(DateTime.UtcNow.Year)
                .Append(year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();
            result.AvailableYears = years;

            return result;
        }
    }
}
