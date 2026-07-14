using BussinessLayer.IServices;
using BussinessLayer.IGateways;
using BussinessLayer.Gateways;
using System;
using System.Collections.Generic;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using Microsoft.Extensions.Configuration;

namespace BussinessLayer.Gateways
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

        public Task<string> CreatePaymentUrl(PaymentRequest request)
        {
            // Tích hợp PayOS SDK: tạo payment link
            // Tham khảo tài liệu PayOS để thực hiện các bước tạo link thanh toán thực tế
            var clientId = _configuration["PayOS:ClientId"];
            var apiKey = _configuration["PayOS:ApiKey"];
            var checksumKey = _configuration["PayOS:ChecksumKey"];
            
            // TODO: Implement actual PayOS integration here
            // Returning a dummy URL for now
            return Task.FromResult($"https://pay.payos.vn/dummy-payment?orderId={request.TransactionId}&amount={request.Amount}");
        }

        public Task<bool> ValidateCallback(IDictionary<string, string> queryParams)
        {
            // Xử lý webhook/callback, validate signature
            // TODO: Implement actual PayOS signature validation
            return Task.FromResult(queryParams.ContainsKey("code") && queryParams["code"] == "00");
        }
    }
}

