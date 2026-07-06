using System.Threading.Tasks;
using System.Collections.Generic;

namespace BussinessLayer.Services
{
    public interface IEmailService
    {
        Task SendAccountCreatedEmailAsync(string toEmail, string username, string password, string role);
        Task SendBroadcastEmailAsync(IEnumerable<string> toEmails, string subject, string content);
    }
}
