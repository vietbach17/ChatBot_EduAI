using BussinessLayer.IServices;
using BussinessLayer.IGateways;
using BussinessLayer.Gateways;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer.Repositories;
using DataAccessLayer.IRepositories;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Dịch vụ Quản lý Gói đăng ký (Subscription). Xử lý logic nâng cấp gói, kiểm tra và reset hạn mức token AI (chu kỳ 5h và hàng tháng), xử lý thanh toán thành công, và các thao tác Admin.
    /// </summary>
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPaymentTransactionRepository _paymentTransactionRepository;
        private readonly IEmailService _emailService;

        public SubscriptionService(
            IUserRepository userRepository,
            IPaymentTransactionRepository paymentTransactionRepository,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _paymentTransactionRepository = paymentTransactionRepository;
            _emailService = emailService;
        }

        public async Task<SubscriptionInfoDto> GetSubscriptionInfoAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return new SubscriptionInfoDto();

            var now = DateTime.UtcNow;
            if (user.QuotaResetDate.HasValue && now >= user.QuotaResetDate.Value)
            {
                user.MonthlyTokensUsed = 0;
                user.QuotaResetDate = now.AddDays(30);
                await _userRepository.UpdateUserAsync(user);
            }

            if (user.ShortTermResetDate.HasValue && now >= user.ShortTermResetDate.Value)
            {
                user.ShortTermTokensUsed = 0;
                user.ShortTermResetDate = now.AddHours(5);
                await _userRepository.UpdateUserAsync(user);
            }

            bool planActive = user.SubscriptionPlan == "Basic" || user.SubscriptionPlan == "Free" ||
                              (user.SubscriptionExpiry.HasValue && user.SubscriptionExpiry.Value >= now);
            string effectivePlan = planActive ? user.SubscriptionPlan : "Basic";
            if (effectivePlan == "Free") effectivePlan = "Basic";

            long limit = TokenQuota.GetShortTermTokenLimit(effectivePlan);
            long remaining = limit == long.MaxValue ? long.MaxValue : Math.Max(0, limit - user.ShortTermTokensUsed);

            long limitMonth = TokenQuota.GetMonthlyTokenLimit(effectivePlan);
            long remainingMonth = limitMonth == long.MaxValue ? long.MaxValue : Math.Max(0, limitMonth - user.MonthlyTokensUsed);

            return new SubscriptionInfoDto
            {
                CurrentPlan   = effectivePlan,
                MonthlyLimit  = limitMonth,
                UsedCount     = user.MonthlyTokensUsed,
                Remaining     = remainingMonth,
                MonthlyResetDate = user.QuotaResetDate,

                ShortTermLimit = limit,
                ShortTermUsedCount = user.ShortTermTokensUsed,
                ShortTermRemaining = remaining,
                ResetDate = user.ShortTermResetDate,

                Expiry        = user.SubscriptionExpiry,
                IsActive      = planActive,

                ExtraQuota    = user.ExtraTokenQuota,
                UseExtraQuota = user.UseExtraQuota
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

            user.MonthlyTokensUsed = 0;
            user.QuotaResetDate = DateTime.UtcNow.AddDays(30);
            user.ShortTermTokensUsed = 0;
            user.ShortTermResetDate = DateTime.UtcNow.AddHours(5);

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
                user.QuotaResetDate = now.AddDays(30);
                user.MonthlyTokensUsed = 0;
                changed = true;
            }
            else if (now >= user.QuotaResetDate.Value)
            {
                user.QuotaResetDate = now.AddDays(30);
                user.MonthlyTokensUsed = 0;
                changed = true;
            }

            if (user.ShortTermResetDate == null)
            {
                user.ShortTermResetDate = now.AddHours(5);
                user.ShortTermTokensUsed = 0;
                changed = true;
            }
            else if (now >= user.ShortTermResetDate.Value)
            {
                user.ShortTermResetDate = now.AddHours(5);
                user.ShortTermTokensUsed = 0;
                changed = true;
            }

            if (changed)
            {
                await _userRepository.UpdateUserAsync(user);
            }

            return true;
        }

        public async Task<bool> ProcessPaymentSuccessAsync(int transactionId, string transactionCode, string? senderAccountInfo = null, string? actualTransferContent = null)
        {
            var transaction = await _paymentTransactionRepository.GetByIdAsync(transactionId);
            if (transaction == null) return false;
            
            // Nếu đã thành công trước đó (webhook gọi lại) thì báo OK luôn
            if (transaction.Status == "Success") return true;
            
            if (transaction.Status != "Pending") return false;

            transaction.Status = "Success";
            transaction.TransactionCode = transactionCode;
            
            if (!string.IsNullOrEmpty(senderAccountInfo))
                transaction.SenderAccountInfo = senderAccountInfo;
                
            if (!string.IsNullOrEmpty(actualTransferContent))
                transaction.ActualTransferContent = actualTransferContent;

            await _paymentTransactionRepository.UpdateAsync(transaction);

            var user = await _userRepository.GetUserByIdAsync(transaction.UserId);
            if (user == null) return false;

            if (transaction.AddonId.HasValue && transaction.AddonPackage != null)
            {
                user.ExtraTokenQuota += transaction.AddonPackage.QuotaAmount;
                await _userRepository.UpdateUserAsync(user);
                
                try { await _emailService.SendInvoiceEmailAsync(transaction); } catch { /* ignore email error */ }
                return true;
            }

            var plan = transaction.SubscriptionPlan;
            if (plan == null) return false;

            user.SubscriptionPlan = plan.Name;

            var now = DateTime.UtcNow;
            var baseDate = (user.SubscriptionExpiry.HasValue && user.SubscriptionExpiry.Value > now)
                ? user.SubscriptionExpiry.Value : now;
            user.SubscriptionExpiry = baseDate.AddDays(plan.DurationDays);

            // Reset Quota ngay lập tức
            user.MonthlyTokensUsed = 0;
            user.QuotaResetDate = DateTime.UtcNow.AddDays(30);
            user.ShortTermTokensUsed = 0;
            user.ShortTermResetDate = DateTime.UtcNow.AddHours(5);

            await _userRepository.UpdateUserAsync(user);
            
            try { await _emailService.SendInvoiceEmailAsync(transaction); } catch { /* ignore email error */ }
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

                long limit = TokenQuota.GetShortTermTokenLimit(effectivePlan);
                long limitMonth = TokenQuota.GetMonthlyTokenLimit(effectivePlan);
                return new UserSubscriptionDto
                {
                    UserId               = u.Id,
                    Username             = u.Username,
                    Email                = u.Email,
                    Role                 = u.Role,
                    SubscriptionPlan     = effectivePlan,
                    SubscriptionExpiry   = u.SubscriptionExpiry,

                    MonthlyTokensUsed    = u.MonthlyTokensUsed,
                    MonthlyLimit         = limitMonth,
                    QuotaResetDate       = u.QuotaResetDate,

                    ShortTermTokensUsed  = u.ShortTermTokensUsed,
                    ShortTermLimit       = limit,
                    ShortTermResetDate   = u.ShortTermResetDate,

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
            
            user.MonthlyTokensUsed = 0;
            user.QuotaResetDate = DateTime.UtcNow.AddDays(30);
            user.ShortTermTokensUsed = 0;
            user.ShortTermResetDate = DateTime.UtcNow.AddHours(5);
            
            await _userRepository.UpdateUserAsync(user);
            return true;
        }

        public async Task<bool> AdminResetQuotaAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            user.MonthlyTokensUsed = 0;
            user.QuotaResetDate = DateTime.UtcNow.AddDays(30);
            user.ShortTermTokensUsed = 0;
            user.ShortTermResetDate = DateTime.UtcNow.AddHours(5);
            await _userRepository.UpdateUserAsync(user);
            return true;
        }

        public async Task<bool> AdminRevokePlanAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            user.SubscriptionPlan   = "Basic";
            user.SubscriptionExpiry = null;
            user.MonthlyTokensUsed = 0;
            user.QuotaResetDate = DateTime.UtcNow.AddDays(30);
            user.ShortTermTokensUsed = 0;
            user.ShortTermResetDate = DateTime.UtcNow.AddHours(5);
            await _userRepository.UpdateUserAsync(user);
            return true;
        }
    }
}

