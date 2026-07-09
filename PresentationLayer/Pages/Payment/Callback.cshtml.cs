using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using BussinessLayer.Services;
using BussinessLayer.IServices;

namespace PresentationLayer.Pages.Payment
{
    public class CallbackModel : PageModel
    {
        private readonly IVNPayService _vnPayService;
        private readonly IConfiguration _configuration;
        private readonly ISubscriptionService _subscriptionService;

        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; } = false;

        public CallbackModel(IVNPayService vnPayService, IConfiguration configuration, ISubscriptionService subscriptionService)
        {
            _vnPayService = vnPayService;
            _configuration = configuration;
            _subscriptionService = subscriptionService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var queryDictionary = HttpContext.Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
            
            var vnp_HashSecret = _configuration["VNPay:HashSecret"] ?? string.Empty;

            if (_vnPayService.ValidateSignature(queryDictionary, vnp_HashSecret))
            {
                var vnp_ResponseCode = queryDictionary.ContainsKey("vnp_ResponseCode") ? queryDictionary["vnp_ResponseCode"] : string.Empty;
                var vnp_TxnRef = queryDictionary.ContainsKey("vnp_TxnRef") ? queryDictionary["vnp_TxnRef"] : string.Empty;
                
                // txnRef format: {UserId}_{Ticks}_{PlanName}
                var parts = vnp_TxnRef.Split('_');

                if (vnp_ResponseCode == "00" && parts.Length >= 3)
                {
                    if (int.TryParse(parts[0], out int userId))
                    {
                        string planName = parts[2];
                        // Process the subscription upgrade
                        await _subscriptionService.UpgradePlanAsync(userId, planName);
                        
                        Message = $"Thanh toán thành công. Đã nâng cấp lên gói {planName}.";
                        IsSuccess = true;
                    }
                    else
                    {
                        Message = "Lỗi xử lý thông tin đơn hàng.";
                    }
                }
                else
                {
                    Message = "Thanh toán thất bại hoặc đã bị hủy.";
                }
            }
            else
            {
                Message = "Chữ ký không hợp lệ. Giao dịch có thể đã bị giả mạo.";
            }

            return Page();
        }
    }
}
