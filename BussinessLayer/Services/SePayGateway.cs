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

        public string CreatePaymentUrl(PaymentRequest request)
        {
            // Tích hợp SePay API: tạo QR / payment URL
            // Tham khảo tài liệu SePay để thực hiện
            var apiKey = _configuration["SePay:ApiKey"];
            
            // TODO: Implement actual SePay integration here
            // Returning a dummy URL for now
            return $"https://sepay.vn/dummy-qr?orderId={request.OrderId}&amount={request.Amount}";
        }

        public bool ValidateCallback(Dictionary<string, string> queryParams)
        {
            // Xử lý webhook callback xác nhận chuyển khoản
            // TODO: Implement actual SePay signature validation
            return queryParams.ContainsKey("status") && queryParams["status"] == "success";
        }
    }
}
