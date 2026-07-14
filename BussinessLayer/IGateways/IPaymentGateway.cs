using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IGateways
{
    /// <summary>
    /// Giao diện chung cho các cổng thanh toán (VNPay, PayOS, SePay).
    /// Định nghĩa việc tạo URL thanh toán và xác thực kết quả trả về (callback).
    /// </summary>
    public interface IPaymentGateway
    {
        string GetGatewayName(); // Trả về "VNPay", "PayOS", "SePay"
        Task<string> CreatePaymentUrl(PaymentRequest request);
        Task<bool> ValidateCallback(IDictionary<string, string> queryParams);
    }
}
