using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer.Repositories;
using DataAccessLayer.IRepositories;

namespace BussinessLayer.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IUserRepository _userRepository;

        public SubscriptionService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        private static int GetMonthlyLimit(string plan) => plan switch
        {
            "Basic" => 100,
            "Premium" => int.MaxValue,
            _ => 5
        };

        public async Task<SubscriptionInfoDto> GetSubscriptionInfoAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return new SubscriptionInfoDto();

            var now = DateTime.UtcNow;
            if (user.QuotaResetDate == null || now >= user.QuotaResetDate)
            {
                user.MonthlyQuestionCount = 0;
                user.QuotaResetDate = DateTime.SpecifyKind(
                    new DateTime(now.Year, now.Month, 1).AddMonths(1), DateTimeKind.Utc);
                await _userRepository.UpdateUserAsync(user);
            }

            bool planActive = user.SubscriptionPlan == "Free" ||
                              (user.SubscriptionExpiry.HasValue && user.SubscriptionExpiry.Value >= now);
            string effectivePlan = planActive ? user.SubscriptionPlan : "Free";
            int limit = GetMonthlyLimit(effectivePlan);
            int remaining = limit == int.MaxValue ? int.MaxValue : Math.Max(0, limit - user.MonthlyQuestionCount);

            return new SubscriptionInfoDto
            {
                CurrentPlan   = effectivePlan,
                MonthlyLimit  = limit,
                UsedCount     = user.MonthlyQuestionCount,
                Remaining     = remaining,
                Expiry        = user.SubscriptionExpiry,
                IsActive      = planActive,
                ResetDate     = user.QuotaResetDate
            };
        }

        public async Task<bool> UpgradePlanAsync(int userId, string plan)
        {
            if (plan != "Basic" && plan != "Premium") return false;
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            var now = DateTime.UtcNow;
            var baseDate = (user.SubscriptionExpiry.HasValue && user.SubscriptionExpiry.Value > now)
                ? user.SubscriptionExpiry.Value : now;

            user.SubscriptionPlan   = plan;
            user.SubscriptionExpiry = baseDate.AddMonths(1);
            await _userRepository.UpdateUserAsync(user);
            return true;
        }

        // ── Admin operations ────────────────────────────────────────────────

        public async Task<IEnumerable<UserSubscriptionDto>> GetAllSubscriptionsAsync()
        {
            var users = await _userRepository.GetAllUsersAsync(false);
            var now = DateTime.UtcNow;
            return users.Select(u =>
            {
                bool planActive = u.SubscriptionPlan == "Free" ||
                                  (u.SubscriptionExpiry.HasValue && u.SubscriptionExpiry.Value >= now);
                string effectivePlan = planActive ? u.SubscriptionPlan : "Free";
                int limit = GetMonthlyLimit(effectivePlan);
                return new UserSubscriptionDto
                {
                    UserId               = u.Id,
                    Username             = u.Username,
                    Email                = u.Email,
                    Role                 = u.Role,
                    SubscriptionPlan     = effectivePlan,
                    SubscriptionExpiry   = u.SubscriptionExpiry,
                    MonthlyQuestionCount = u.MonthlyQuestionCount,
                    MonthlyLimit         = limit,
                    QuotaResetDate       = u.QuotaResetDate,
                    IsActive             = planActive
                };
            }).ToList();
        }

        public async Task<bool> AdminSetPlanAsync(int userId, string plan, DateTime? expiry)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            user.SubscriptionPlan   = plan;
            user.SubscriptionExpiry = plan == "Free" ? null : expiry;
            await _userRepository.UpdateUserAsync(user);
            return true;
        }

        public async Task<bool> AdminResetQuotaAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            var now = DateTime.UtcNow;
            user.MonthlyQuestionCount = 0;
            user.QuotaResetDate = DateTime.SpecifyKind(
                new DateTime(now.Year, now.Month, 1).AddMonths(1), DateTimeKind.Utc);
            await _userRepository.UpdateUserAsync(user);
            return true;
        }

        public async Task<bool> AdminRevokePlanAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            user.SubscriptionPlan   = "Free";
            user.SubscriptionExpiry = null;
            await _userRepository.UpdateUserAsync(user);
            return true;
        }
    }
}
