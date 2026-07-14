using System.Threading.Tasks;
using System.Collections.Generic;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Giao diện dịch vụ Gửi Email: OTP, chào mừng, hóa đơn thanh toán.
    /// </summary>
    public interface IEmailService
    {
        Task SendAccountCreatedEmailAsync(string toEmail, string username, string password, string role);
        Task SendBroadcastEmailAsync(IEnumerable<string> toEmails, string subject, string content);
        Task SendInvoiceEmailAsync(DataAccessLayer.Entities.PaymentTransaction transaction);
        Task SendPasswordResetOtpAsync(string toEmail, string otp);
    }
}
