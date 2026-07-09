using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using DataAccessLayer.IRepositories;

namespace BussinessLayer.Services
{
    public class PaymentHistoryService : IPaymentHistoryService
    {
        private readonly IPaymentTransactionRepository _transactionRepository;

        public PaymentHistoryService(IPaymentTransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<IEnumerable<PaymentHistoryDto>> GetPaymentHistoryByUserIdAsync(int userId)
        {
            var transactions = await _transactionRepository.GetByUserIdAsync(userId);
            if (transactions == null) return new List<PaymentHistoryDto>();

            return transactions.Select(t => new PaymentHistoryDto
            {
                TransactionId = t.Id,
                TransactionCode = t.TransactionCode,
                UserName = t.User != null ? t.User.Username : "Unknown",
                PlanName = t.SubscriptionPlan != null ? t.SubscriptionPlan.Name : "Unknown",
                Amount = t.Amount,
                Method = t.PaymentMethod,
                Status = t.Status,
                Date = t.CreatedAt
            }).OrderByDescending(x => x.Date).ToList();
        }

        public async Task<IEnumerable<PaymentHistoryDto>> GetAllPaymentHistoriesAsync(string searchTerm = null, string method = null, string status = null)
        {
            var transactions = await _transactionRepository.GetAllAsync();
            if (transactions == null) return new List<PaymentHistoryDto>();

            var query = transactions.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(t => t.User != null && (t.User.Username.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || t.User.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrEmpty(method))
            {
                query = query.Where(t => t.PaymentMethod.Equals(method, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            return query.Select(t => new PaymentHistoryDto
            {
                TransactionId = t.Id,
                TransactionCode = t.TransactionCode,
                UserName = t.User != null ? t.User.Username : "Unknown",
                PlanName = t.SubscriptionPlan != null ? t.SubscriptionPlan.Name : "Unknown",
                Amount = t.Amount,
                Method = t.PaymentMethod,
                Status = t.Status,
                Date = t.CreatedAt
            }).OrderByDescending(x => x.Date).ToList();
        }

        public async Task<InvoiceDto> GetInvoiceDetailsAsync(int transactionId)
        {
            // Note: In a real implementation, we would need a GetByIdAsync in the repository
            var transactions = await _transactionRepository.GetAllAsync();
            var t = transactions?.FirstOrDefault(x => x.Id == transactionId);
            if (t == null) return null;

            return new InvoiceDto
            {
                TransactionId = t.Id,
                TransactionCode = t.TransactionCode,
                UserName = t.User?.Username ?? "Unknown",
                UserEmail = t.User?.Email ?? "Unknown",
                PlanName = t.SubscriptionPlan?.Name ?? "Unknown",
                Amount = t.Amount,
                PaymentMethod = t.PaymentMethod,
                Status = t.Status,
                PaymentDate = t.CreatedAt,
                ExpiryDate = t.ExpiryDate
            };
        }
    }
}
