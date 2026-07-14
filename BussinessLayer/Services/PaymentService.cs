using BussinessLayer.IServices;
using BussinessLayer.IGateways;
using BussinessLayer.Gateways;
using System;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Dịch vụ Thanh toán. Tạo giao dịch (PaymentTransaction), tạo URL thanh toán thông qua các cổng VNPay/PayOS/SePay, và cập nhật trạng thái giao dịch.
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentTransactionRepository _transactionRepository;
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly IAddonPackageRepository _addonRepository;

        public PaymentService(
            IPaymentTransactionRepository transactionRepository,
            ISubscriptionPlanRepository planRepository,
            IAddonPackageRepository addonRepository)
        {
            _transactionRepository = transactionRepository;
            _planRepository = planRepository;
            _addonRepository = addonRepository;
        }

        public async Task<PaymentTransactionDto> CreateTransactionAsync(int userId, int planId, string paymentMethod)
        {
            var plan = await _planRepository.GetByIdAsync(planId);
            if (plan == null)
            {
                throw new ArgumentException("Gói cước không tồn tại.");
            }

            var transaction = new PaymentTransaction
            {
                UserId = userId,
                PlanId = planId,
                Amount = plan.Price,
                PaymentMethod = paymentMethod,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(plan.DurationDays)
            };

            await _transactionRepository.AddAsync(transaction);

            return MapToDto(transaction);
        }

        public async Task<PaymentTransactionDto> CreateAddonTransactionAsync(int userId, int addonId, string paymentMethod)
        {
            var addon = await _addonRepository.GetByIdAsync(addonId);
            if (addon == null || !addon.IsActive)
            {
                throw new ArgumentException("Gói nạp thêm không tồn tại hoặc đã ngừng bán.");
            }

            var transaction = new PaymentTransaction
            {
                UserId = userId,
                AddonId = addonId,
                Amount = addon.Price,
                PaymentMethod = paymentMethod,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                ExpiryDate = null
            };

            await _transactionRepository.AddAsync(transaction);

            return MapToDto(transaction);
        }

        public async Task<PaymentTransactionDto?> GetTransactionByIdAsync(int id)
        {
            var transaction = await _transactionRepository.GetByIdAsync(id);
            if (transaction == null) return null;
            return MapToDto(transaction);
        }

        public async Task<bool> UpdateTransactionStatusAsync(int id, string status, string? transactionCode)
        {
            var transaction = await _transactionRepository.GetByIdAsync(id);
            if (transaction == null) return false;

            transaction.Status = status;
            if (transactionCode != null)
            {
                transaction.TransactionCode = transactionCode;
            }

            await _transactionRepository.UpdateAsync(transaction);
            return true;
        }

        private static PaymentTransactionDto MapToDto(PaymentTransaction t)
        {
            return new PaymentTransactionDto
            {
                Id = t.Id,
                UserId = t.UserId,
                PlanId = t.PlanId,
                AddonId = t.AddonId,
                PlanName = t.SubscriptionPlan?.Name ?? t.AddonPackage?.Name ?? string.Empty,
                Amount = t.Amount,
                PaymentMethod = t.PaymentMethod,
                Status = t.Status,
                TransactionCode = t.TransactionCode,
                CreatedAt = t.CreatedAt,
                ExpiryDate = t.ExpiryDate
            };
        }
    }
}
