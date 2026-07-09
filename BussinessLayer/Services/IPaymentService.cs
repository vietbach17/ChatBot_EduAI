using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.Services
{
    public interface IPaymentService
    {
        Task<PaymentTransactionDto> CreateTransactionAsync(int userId, int planId, string paymentMethod);
        Task<PaymentTransactionDto?> GetTransactionByIdAsync(int id);
        Task<bool> UpdateTransactionStatusAsync(int id, string status, string? transactionCode);
    }
}
