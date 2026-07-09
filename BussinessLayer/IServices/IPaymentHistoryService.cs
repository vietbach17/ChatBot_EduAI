using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    public interface IPaymentHistoryService
    {
        Task<IEnumerable<PaymentHistoryDto>> GetPaymentHistoryByUserIdAsync(int userId);
        Task<IEnumerable<PaymentHistoryDto>> GetAllPaymentHistoriesAsync(string searchTerm = null, string method = null, string status = null);
        Task<InvoiceDto> GetInvoiceDetailsAsync(int transactionId);
    }
}
