using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Giao diện dịch vụ Quản lý Gói đăng ký: nâng cấp, kiểm tra quota, xử lý thanh toán.
    /// </summary>
    public interface ISubscriptionService
    {
        Task<SubscriptionInfoDto> GetSubscriptionInfoAsync(int userId);
        Task<bool> UpgradePlanAsync(int userId, string plan);
        Task<bool> CheckAndUpdateQuotaAsync(int userId);
        Task<bool> ProcessPaymentSuccessAsync(int transactionId, string transactionCode, string? senderAccountInfo = null, string? actualTransferContent = null);

        // Admin operations
        Task<IEnumerable<UserSubscriptionDto>> GetAllSubscriptionsAsync();
        Task<bool> AdminSetPlanAsync(int userId, string plan, DateTime? expiry);
        Task<bool> AdminResetQuotaAsync(int userId);
        Task<bool> AdminRevokePlanAsync(int userId);
    }
}
