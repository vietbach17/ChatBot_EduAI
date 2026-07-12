using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    public interface ISubscriptionService
    {
        Task<SubscriptionInfoDto> GetSubscriptionInfoAsync(int userId);
        Task<bool> UpgradePlanAsync(int userId, string plan);
        Task<bool> CheckAndUpdateQuotaAsync(int userId);
        Task<bool> ProcessPaymentSuccessAsync(int transactionId, string transactionCode);

        // Admin operations
        Task<IEnumerable<UserSubscriptionDto>> GetAllSubscriptionsAsync();
        Task<bool> AdminSetPlanAsync(int userId, string plan, DateTime? expiry);
        Task<bool> AdminResetQuotaAsync(int userId);
        Task<bool> AdminRevokePlanAsync(int userId);
    }
}
