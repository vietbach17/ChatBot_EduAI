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
            
            // Generate checkout url using qr.sepay.vn
            // Prefix the transaction ID with "EDU" for clarity
            var content = $"EDU{request.TransactionId}";
            var amount = Math.Floor(request.Amount); // Amount should be integer

            var url = $"https://qr.sepay.vn/checkout.html?bank={bankId}&acc={accountNo}&amount={amount}&des={content}";
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

