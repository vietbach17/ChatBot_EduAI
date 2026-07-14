using BussinessLayer.IServices;
using BussinessLayer.IGateways;
using BussinessLayer.Gateways;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using BussinessLayer.DTOs;
using BussinessLayer.Helpers;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Dịch vụ tạo URL thanh toán VNPay. Xây dựng chuỗi query parameters theo chuẩn VNPay và ký HMAC-SHA512.
    /// </summary>
    public class VNPayService : IVNPayService
    {
        private readonly IConfiguration _configuration;

        public VNPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(VNPayRequestDto request)
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
            
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", request.IpAddress);
            vnpay.AddRequestData("vnp_Locale", "vn");
            
            vnpay.AddRequestData("vnp_OrderInfo", request.OrderDescription);
            vnpay.AddRequestData("vnp_OrderType", "other"); // Or "topup", "billpayment", etc.
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            
            // Generate a unique transaction id (TxnRef)
            var txnRef = $"{request.UserId}_{DateTime.Now.Ticks}_{request.PlanName}";
            vnpay.AddRequestData("vnp_TxnRef", txnRef);

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

            return paymentUrl;
        }

        public bool ValidateSignature(IDictionary<string, string> responseData, string secretKey)
        {
            var vnpay = new VNPayLibrary();
            foreach (var kvp in responseData)
            {
                if (!string.IsNullOrEmpty(kvp.Key) && kvp.Key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(kvp.Key, kvp.Value);
                }
            }

            var vnp_SecureHash = responseData.ContainsKey("vnp_SecureHash") ? responseData["vnp_SecureHash"] : string.Empty;
            return vnpay.ValidateSignature(vnp_SecureHash, secretKey);
        }
    }
}

