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
        private readonly IPaymentTransactionRepository _paymentTransactionRepository;

        public SubscriptionService(
            IUserRepository userRepository,
            IPaymentTransactionRepository paymentTransactionRepository)
        {
            _userRepository = userRepository;
            _paymentTransactionRepository = paymentTransactionRepository;
        }

        private static int GetMonthlyLimit(string plan) => plan switch
        {
            "Basic" => 5,
            "Pro" => 20,
            "Ultra" => int.MaxValue,
            _ => 5
        };

        public async Task<SubscriptionInfoDto> GetSubscriptionInfoAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return new SubscriptionInfoDto();

            var now = DateTime.UtcNow;
            if (user.QuotaResetDate.HasValue && now >= user.QuotaResetDate.Value)
            {
                user.MonthlyQuestionCount = 0;
                user.QuotaResetDate = null;
                await _userRepository.UpdateUserAsync(user);
            }

            bool planActive = user.SubscriptionPlan == "Basic" || user.SubscriptionPlan == "Free" ||
                              (user.SubscriptionExpiry.HasValue && user.SubscriptionExpiry.Value >= now);
            string effectivePlan = planActive ? user.SubscriptionPlan : "Basic";
            if (effectivePlan == "Free") effectivePlan = "Basic";

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
            if (plan != "Basic" && plan != "Pro" && plan != "Ultra") return false;
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            user.SubscriptionPlan = plan;
            if (plan == "Basic")
            {
                user.SubscriptionExpiry = null;
            }
            else
            {
                var now = DateTime.UtcNow;
                var baseDate = (user.SubscriptionExpiry.HasValue && user.SubscriptionExpiry.Value > now)
                    ? user.SubscriptionExpiry.Value : now;
                user.SubscriptionExpiry = baseDate.AddDays(30);
            }

            user.MonthlyQuestionCount = 0;
            user.QuotaResetDate = null;

            await _userRepository.UpdateUserAsync(user);
            return true;
        }

        public async Task<bool> CheckAndUpdateQuotaAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            var now = DateTime.UtcNow;
            bool changed = false;

            if (user.QuotaResetDate == null)
            {
                user.QuotaResetDate = now.AddHours(5);
                user.MonthlyQuestionCount = 0;
                changed = true;
            }
            else if (now >= user.QuotaResetDate.Value)
            {
                user.QuotaResetDate = now.AddHours(5);
                user.MonthlyQuestionCount = 0;
                changed = true;
            }

            if (changed)
            {
                await _userRepository.UpdateUserAsync(user);
            }

            return true;
        }

        public async Task<bool> ProcessPaymentSuccessAsync(int transactionId, string transactionCode)
        {
            var transaction = await _paymentTransactionRepository.GetByIdAsync(transactionId);
            if (transaction == null || transaction.Status != "Pending") return false;

            transaction.Status = "Success";
            transaction.TransactionCode = transactionCode;
            await _paymentTransactionRepository.UpdateAsync(transaction);

            var user = await _userRepository.GetUserByIdAsync(transaction.UserId);
            if (user == null) return false;

            var plan = transaction.SubscriptionPlan;
            if (plan == null) return false;

            user.SubscriptionPlan = plan.Name;

            var now = DateTime.UtcNow;
            var baseDate = (user.SubscriptionExpiry.HasValue && user.SubscriptionExpiry.Value > now)
                ? user.SubscriptionExpiry.Value : now;
            user.SubscriptionExpiry = baseDate.AddDays(plan.DurationDays);

            // Reset Quota ngay lập tức
            user.MonthlyQuestionCount = 0;
            user.QuotaResetDate = null;

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
                bool planActive = u.SubscriptionPlan == "Basic" || u.SubscriptionPlan == "Free" ||
                                  (u.SubscriptionExpiry.HasValue && u.SubscriptionExpiry.Value >= now);
                string effectivePlan = planActive ? u.SubscriptionPlan : "Basic";
                if (effectivePlan == "Free") effectivePlan = "Basic";

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
            user.SubscriptionExpiry = (plan == "Basic" || plan == "Free") ? null : expiry;
            await _userRepository.UpdateUserAsync(user);
            return true;
        }

        public async Task<bool> AdminResetQuotaAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            user.MonthlyQuestionCount = 0;
            user.QuotaResetDate = null;
            await _userRepository.UpdateUserAsync(user);
            return true;
        }

        public async Task<bool> AdminRevokePlanAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            user.SubscriptionPlan   = "Basic";
            user.SubscriptionExpiry = null;
            user.MonthlyQuestionCount = 0;
            user.QuotaResetDate = null;
            await _userRepository.UpdateUserAsync(user);
            return true;
        }
    }
}
