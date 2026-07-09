using System;
using System.Collections.Generic;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using Microsoft.Extensions.Configuration;

namespace BussinessLayer.Services
{
    public class PayOSGateway : IPaymentGateway
    {
        private readonly IConfiguration _configuration;

        public PayOSGateway(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetGatewayName()
        {
            return "PayOS";
        }

        public string CreatePaymentUrl(PaymentRequest request)
        {
            // Tích hợp PayOS SDK: tạo payment link
            // Tham khảo tài liệu PayOS để thực hiện các bước tạo link thanh toán thực tế
            var clientId = _configuration["PayOS:ClientId"];
            var apiKey = _configuration["PayOS:ApiKey"];
            var checksumKey = _configuration["PayOS:ChecksumKey"];
            
            // TODO: Implement actual PayOS integration here
            // Returning a dummy URL for now
            return $"https://pay.payos.vn/dummy-payment?orderId={request.OrderId}&amount={request.Amount}";
        }

        public bool ValidateCallback(Dictionary<string, string> queryParams)
        {
            // Xử lý webhook/callback, validate signature
            // TODO: Implement actual PayOS signature validation
            return queryParams.ContainsKey("code") && queryParams["code"] == "00";
        }
    }
}
