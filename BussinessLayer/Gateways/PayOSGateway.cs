using BussinessLayer.IServices;
using BussinessLayer.IGateways;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using Microsoft.Extensions.Configuration;
using Net.payOS;
using Net.payOS.Types;

namespace BussinessLayer.Gateways
{
    /// <summary>
    /// Cổng thanh toán PayOS. Tạo link thanh toán qua PayOS SDK và xác thực callback thông qua checksum.
    /// </summary>
    public class PayOSGateway : IPaymentGateway
    {
        private readonly IConfiguration _configuration;
        private readonly Net.payOS.PayOS _payOS;

        public PayOSGateway(IConfiguration configuration)
        {
            _configuration = configuration;
            var clientId = _configuration["PayOS:ClientId"] ?? string.Empty;
            var apiKey = _configuration["PayOS:ApiKey"] ?? string.Empty;
            var checksumKey = _configuration["PayOS:ChecksumKey"] ?? string.Empty;
            _payOS = new Net.payOS.PayOS(clientId, apiKey, checksumKey);
        }

        public string GetGatewayName()
        {
            return "PayOS";
        }

        public async Task<string> CreatePaymentUrl(PaymentRequest request)
        {
            // orderCode must be unique integer
            long orderCode = request.TransactionId; 
            
            // Description max length is 25 chars
            string description = "Thanh toan " + request.TransactionId;
            if (description.Length > 25)
            {
                description = description.Substring(0, 25);
            }
            
            // Amount must be integer
            int amount = (int)request.Amount;
            
            var paymentData = new PaymentData(
                orderCode: orderCode,
                amount: amount,
                description: description,
                items: new List<ItemData> { new ItemData(request.OrderDescription ?? "Plan/Addon", 1, amount) },
                cancelUrl: request.ReturnUrl,
                returnUrl: request.ReturnUrl
            );

            CreatePaymentResult createPayment = await _payOS.createPaymentLink(paymentData);

            return createPayment.checkoutUrl;
        }

        public Task<bool> ValidateCallback(IDictionary<string, string> queryParams)
        {
            return Task.FromResult(queryParams.ContainsKey("code") && queryParams["code"] == "00");
        }
    }
}
