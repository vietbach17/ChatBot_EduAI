using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    public interface IPaymentTransactionRepository
    {
        Task<PaymentTransaction?> GetByIdAsync(int id);
        Task<IEnumerable<PaymentTransaction>> GetByUserIdAsync(int userId);
        Task<IEnumerable<PaymentTransaction>> GetByStatusAsync(string status);
        Task AddAsync(PaymentTransaction transaction);
        Task UpdateAsync(PaymentTransaction transaction);
    }
}
