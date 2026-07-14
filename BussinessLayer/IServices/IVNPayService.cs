using System.Collections.Generic;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Giao diện dịch vụ tạo URL thanh toán VNPay.
    /// </summary>
    public interface IVNPayService
    {
        string CreatePaymentUrl(VNPayRequestDto request);
        bool ValidateSignature(IDictionary<string, string> responseData, string secretKey);
    }
}
