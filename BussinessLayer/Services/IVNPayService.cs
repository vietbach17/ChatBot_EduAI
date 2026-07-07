using System.Collections.Generic;
using BussinessLayer.DTOs;

namespace BussinessLayer.Services
{
    public interface IVNPayService
    {
        string CreatePaymentUrl(VNPayRequestDto request);
        bool ValidateSignature(IDictionary<string, string> responseData, string secretKey);
    }
}
