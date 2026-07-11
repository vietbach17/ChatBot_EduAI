using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IGateways
{
    public interface IPaymentGateway
    {
        string GetGatewayName(); // Trả về "VNPay", "PayOS", "SePay"
        Task<string> CreatePaymentUrl(PaymentRequest request);
        Task<bool> ValidateCallback(IDictionary<string, string> queryParams);
    }
}
