using BussinessLayer.IServices;
using BussinessLayer.IGateways;
using BussinessLayer.Gateways;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using BussinessLayer.DTOs;
using BussinessLayer.Helpers;

namespace BussinessLayer.Gateways
{
    public class VNPayGateway : IPaymentGateway
    {
        private readonly IConfiguration _configuration;

        public VNPayGateway(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetGatewayName() => "VNPay";

        public Task<string> CreatePaymentUrl(PaymentRequest request)
        {
            var vnpay = new VNPayLibrary();

            var vnp_TmnCode = _configuration["VNPay:TmnCode"] ?? string.Empty;
            var vnp_HashSecret = _configuration["VNPay:HashSecret"] ?? string.Empty;
            var vnp_Url = _configuration["VNPay:BaseUrl"] ?? string.Empty;
            var vnp_Returnurl = request.ReturnUrl;

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", ((long)(request.Amount * 100)).ToString()); // VNPay amount is in VND * 100
            
            vnpay.AddRequestData("vnp_CreateDate", DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss")); // Local Vietnam time is UTC+7
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", request.IpAddress);
            vnpay.AddRequestData("vnp_Locale", "vn");
            
            vnpay.AddRequestData("vnp_OrderInfo", request.OrderDescription);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            
            // Generate a unique transaction id (TxnRef) of format TransactionId_Ticks
            var txnRef = $"{request.TransactionId}_{DateTime.UtcNow.Ticks}";
            vnpay.AddRequestData("vnp_TxnRef", txnRef);

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

            return Task.FromResult(paymentUrl);
        }

        public Task<bool> ValidateCallback(IDictionary<string, string> queryParams)
        {
            var vnpay = new VNPayLibrary();
            foreach (var kvp in queryParams)
            {
                if (!string.IsNullOrEmpty(kvp.Key) && kvp.Key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(kvp.Key, kvp.Value);
                }
            }

            var vnp_HashSecret = _configuration["VNPay:HashSecret"] ?? string.Empty;
            var vnp_SecureHash = queryParams.ContainsKey("vnp_SecureHash") ? queryParams["vnp_SecureHash"] : string.Empty;
            bool isValid = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
            return Task.FromResult(isValid);
        }
    }
}

