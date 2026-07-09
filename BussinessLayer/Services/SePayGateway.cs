using System;
using System.Collections.Generic;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using Microsoft.Extensions.Configuration;

namespace BussinessLayer.Services
{
    public class SePayGateway : IPaymentGateway
    {
        private readonly IConfiguration _configuration;

        public SePayGateway(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetGatewayName()
        {
            return "SePay";
        }

        public Task<string> CreatePaymentUrl(PaymentRequest request)
        {
            // Tích hợp SePay API: tạo QR / payment URL
            // Tham khảo tài liệu SePay để thực hiện
            var apiKey = _configuration["SePay:ApiKey"];
            
            // TODO: Implement actual SePay integration here
            // Returning a dummy URL for now
            return Task.FromResult($"https://sepay.vn/dummy-qr?orderId={request.TransactionId}&amount={request.Amount}");
        }

        public Task<bool> ValidateCallback(IDictionary<string, string> queryParams)
        {
            // Xử lý webhook callback xác nhận chuyển khoản
            // TODO: Implement actual SePay signature validation
            return Task.FromResult(queryParams.ContainsKey("status") && queryParams["status"] == "success");
        }
    }
}
