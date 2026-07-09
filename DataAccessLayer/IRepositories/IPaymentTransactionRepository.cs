using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    public interface IPaymentTransactionRepository
    {
        Task<PaymentTransaction> GetByIdAsync(int id);
        Task<PaymentTransaction> GetByTxnRefAsync(string txnRef);
        Task<IEnumerable<PaymentTransaction>> GetAllAsync();
        Task<IEnumerable<PaymentTransaction>> GetByUserIdAsync(int userId);
        Task AddAsync(PaymentTransaction transaction);
        Task UpdateAsync(PaymentTransaction transaction);
    }
}
