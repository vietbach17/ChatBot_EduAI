using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using BussinessLayer.IServices;
using BussinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.SignalR;
using Net.payOS;
using Net.payOS.Types;

namespace PresentationLayer.Controllers
{
    [Route("api/payments")]
    [ApiController]
    [AllowAnonymous] // Webhook luôn phải là public để PayOS có thể gọi tới
    /// <summary>
    /// API Controller xử lý Webhook từ PayOS.
    /// Nhận kết quả thanh toán, xác thực chữ ký (checksum), cập nhật trạng thái đơn hàng và tự động cấp quyền lợi.
    /// </summary>
    public class PayOSWebhookController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IPaymentService _paymentService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IHubContext<SignalRHub> _hubContext;

        public PayOSWebhookController(
            IConfiguration configuration,
            IPaymentService paymentService,
            ISubscriptionService subscriptionService,
            IHubContext<SignalRHub> hubContext)
        {
            _configuration = configuration;
            _paymentService = paymentService;
            _subscriptionService = subscriptionService;
            _hubContext = hubContext;
        }

        // POST: api/payments/webhook
        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveWebhook([FromBody] WebhookType payload)
        {
            try
            {
                // 1. Khởi tạo PayOS với cấu hình hiện tại để dùng hàm verify
                var clientId = _configuration["PayOS:ClientId"] ?? string.Empty;
                var apiKey = _configuration["PayOS:ApiKey"] ?? string.Empty;
                var checksumKey = _configuration["PayOS:ChecksumKey"] ?? string.Empty;
                var payOS = new Net.payOS.PayOS(clientId, apiKey, checksumKey);

                // 2. Xác thực Webhook (Nếu checksum không đúng, hàm này sẽ ném ra Exception)
                WebhookData data = payOS.verifyPaymentWebhookData(payload);

                // Nếu verify thành công, tiếp tục kiểm tra kết quả giao dịch
                if (data.code == "00")
                {
                    // Trích xuất mã đơn hàng (transactionId)
                    int transactionId = (int)data.orderCode;

                    // Kiểm tra trạng thái đơn hàng trong DB
                    var transaction = await _paymentService.GetTransactionByIdAsync(transactionId);
                    
                    if (transaction != null && transaction.Status == "Pending")
                    {
                        // 3. Thanh toán thành công, gọi hàm nâng cấp tài khoản
                        bool processed = await _subscriptionService.ProcessPaymentSuccessAsync(
                            transactionId,
                            data.reference ?? "PayOS",
                            "PayOS",
                            data.description
                        );

                        if (processed)
                        {
                            // Thông báo tới client qua SignalR để reload giao diện ngay lập tức
                            await _hubContext.Clients.All.SendAsync("PaymentStatusUpdated", transactionId, "Success");
                        }
                    }
                }

                // Luôn trả về 200 OK để PayOS biết là server đã nhận được webhook và không gọi lại
                return Ok(new { success = true, message = "Webhook received and verified successfully" });
            }
            catch (Exception ex)
            {
                // Trong trường hợp verify lỗi (ví dụ: chữ ký không hợp lệ, dữ liệu giả mạo)
                // Cần trả về BadRequest hoặc Ok tuỳ chiến lược (PayOS khuyến cáo trả về 200 OK kèm error để họ khỏi retry nếu đó là request rác)
                Console.WriteLine($"[PayOS Webhook Error]: {ex.Message}");
                return Ok(new { success = false, message = "Webhook verification failed or error processing" });
            }
        }
    }
}
