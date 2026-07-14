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
    /// <summary>
    /// Cổng thanh toán SePay (Chuyển khoản ngân hàng). Tạo mã QR chuyển khoản với nội dung tự động nhận diện giao dịch.
    /// </summary>
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
            var bankId = _configuration["SePay:BankId"] ?? "MB";
            var accountNo = _configuration["SePay:AccountNumber"] ?? "0000000000";
            
            var content = $"EDU{request.TransactionId}";
            var amount = Math.Floor(request.Amount); // Amount should be integer
            
            // Redirect to our internal SePayCheckout page instead of external QR tool
            string safeReturnUrl = request.ReturnUrl ?? "/";
            var url = $"/Payment/SePayCheckout?bankId={bankId}&accountNo={accountNo}&amount={amount}&content={content}&returnUrl={Uri.EscapeDataString(safeReturnUrl)}";
            return Task.FromResult(url);
        }

        public Task<bool> ValidateCallback(IDictionary<string, string> queryParams)
        {
            // For SePay, Webhook handles the confirmation asynchronously.
            // This sync ValidateCallback from URL is generally not applicable unless SePay redirects with params.
            return Task.FromResult(false);
        }
    }
}

