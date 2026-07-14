using System.Collections.Generic;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    public interface IVNPayService
    {
        string CreatePaymentUrl(VNPayRequestDto request);
        bool ValidateSignature(IDictionary<string, string> responseData, string secretKey);
    }
}
