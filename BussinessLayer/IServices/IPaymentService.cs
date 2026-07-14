using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Giao diện dịch vụ Thanh toán: tạo giao dịch, tạo URL, cập nhật trạng thái.
    /// </summary>
    public interface IPaymentService
    {
        Task<PaymentTransactionDto> CreateTransactionAsync(int userId, int planId, string paymentMethod);
        Task<PaymentTransactionDto> CreateAddonTransactionAsync(int userId, int addonId, string paymentMethod);
        Task<PaymentTransactionDto?> GetTransactionByIdAsync(int id);
        Task<bool> UpdateTransactionStatusAsync(int id, string status, string? transactionCode);
    }
}
