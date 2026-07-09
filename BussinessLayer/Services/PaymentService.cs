using System;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;

namespace BussinessLayer.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentTransactionRepository _transactionRepository;
        private readonly ISubscriptionPlanRepository _planRepository;

        public PaymentService(
            IPaymentTransactionRepository transactionRepository,
            ISubscriptionPlanRepository planRepository)
        {
            _transactionRepository = transactionRepository;
            _planRepository = planRepository;
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
                PlanName = t.SubscriptionPlan?.Name ?? string.Empty,
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
