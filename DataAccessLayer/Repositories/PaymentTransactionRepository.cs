using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public class PaymentTransactionRepository : IPaymentTransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentTransactionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaymentTransaction?> GetByIdAsync(int id)
            => await _context.PaymentTransactions.Include(t => t.SubscriptionPlan).FirstOrDefaultAsync(t => t.Id == id);

        public async Task<IEnumerable<PaymentTransaction>> GetByUserIdAsync(int userId)
            => await _context.PaymentTransactions.Include(t => t.SubscriptionPlan).Where(t => t.UserId == userId).OrderByDescending(t => t.CreatedAt).ToListAsync();

        public async Task<IEnumerable<PaymentTransaction>> GetByStatusAsync(string status)
            => await _context.PaymentTransactions.Where(t => t.Status == status).ToListAsync();

        public async Task AddAsync(PaymentTransaction transaction)
        {
            _context.PaymentTransactions.Add(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PaymentTransaction transaction)
        {
            _context.PaymentTransactions.Update(transaction);
            await _context.SaveChangesAsync();
        }
    }
}
