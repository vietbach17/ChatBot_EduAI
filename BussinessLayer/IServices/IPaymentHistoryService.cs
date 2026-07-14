using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Giao diện dịch vụ Lịch sử Thanh toán.
    /// </summary>
    public interface IPaymentHistoryService
    {
        Task<IEnumerable<PaymentHistoryDto>> GetPaymentHistoryByUserIdAsync(int userId);
        Task<IEnumerable<PaymentHistoryDto>> GetAllPaymentHistoriesAsync(string searchTerm = null, string method = null, string status = null);
        Task<InvoiceDto> GetInvoiceDetailsAsync(int transactionId);
        Task<bool> CancelPaymentAsync(int transactionId, int userId);
    }
}
