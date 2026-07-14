using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Text.Json;
using BussinessLayer.IServices;
using BussinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.SignalR;

namespace PresentationLayer.Controllers
{
    [Route("api/sepay")]
    [ApiController]
    [AllowAnonymous] // Webhooks must be public
    /// <summary>
    /// API Controller xử lý Webhook từ SePay. Nhận thông báo chuyển khoản ngân hàng, xác thực API Key, trích xuất mã giao dịch, và tự động cấp quyền lợi.
    /// </summary>
    public class SePayWebhookController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IPaymentService _paymentService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IHubContext<SignalRHub> _hubContext;

        public SePayWebhookController(
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

        // POST: api/sepay/webhook
        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveWebhook([FromBody] SePayWebhookPayload payload)
        {
            // 1. Verify ApiKey from Headers
            if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                return Unauthorized(new { success = false, message = "Missing Authorization header" });
            }

            var expectedApiKey = _configuration["SePay:ApiKey"];
            var providedApiKey = authHeader.ToString().Replace("Apikey ", "").Trim();

            if (string.IsNullOrEmpty(expectedApiKey) || providedApiKey != expectedApiKey)
            {
                return Unauthorized(new { success = false, message = "Invalid API Key" });
            }

            // 2. Validate payload
            if (payload == null || string.IsNullOrEmpty(payload.content))
            {
                // Return 200 OK for test webhooks to ensure SePay accepts the URL
                return Ok(new { success = true, message = "Webhook connection test successful" });
            }

            // 3. Extract transaction ID from content (e.g., "EDU123" -> 123)
            var contentUpper = payload.content.ToUpper();
            int startIndex = contentUpper.IndexOf("EDU");
            if (startIndex == -1)
            {
                // Not an EDU transaction, ignore but return OK so SePay doesn't retry
                return Ok(new { success = true, message = "Ignored non-EDU transaction" });
            }

            string possibleIdStr = "";
            for (int i = startIndex + 3; i < contentUpper.Length; i++)
            {
                if (char.IsDigit(contentUpper[i]))
                {
                    possibleIdStr += contentUpper[i];
                }
                else if (possibleIdStr.Length > 0)
                {
                    break; // stop at first non-digit after digits
                }
            }

            if (!int.TryParse(possibleIdStr, out int transactionId))
            {
                return Ok(new { success = true, message = "Could not parse transaction ID" });
            }

            // 4. Verify transaction exists and amount matches
            var transaction = await _paymentService.GetTransactionByIdAsync(transactionId);
            if (transaction == null || transaction.Status != "Pending")
            {
                return Ok(new { success = true, message = "Transaction not found or already processed" });
            }

            // SePay payload 'transferAmount' is the transferred money
            if (payload.transferAmount < transaction.Amount)
            {
                // Partially paid, mark as failed or log it. We just log and ignore for now.
                return Ok(new { success = true, message = "Insufficient amount transferred" });
            }

            // 5. Success! Upgrade subscription
            bool processed = await _subscriptionService.ProcessPaymentSuccessAsync(
                transactionId, 
                payload.referenceCode, 
                payload.subAccount, 
                payload.content);
            
            if (processed)
            {
                await _hubContext.Clients.All.SendAsync("PaymentStatusUpdated", transactionId, "Success");
                return Ok(new { success = true, message = "Payment processed successfully" });
            }
            else
            {
                return StatusCode(500, new { success = false, message = "Failed to process subscription" });
            }
        }
    }

    /// <summary>Cấu trúc dữ liệu Webhook do SePay gửi về khi có giao dịch chuyển khoản ngân hàng.</summary>
    public class SePayWebhookPayload
    {
        public int id { get; set; }
        public string? gateway { get; set; }
        public string? transactionDate { get; set; }
        public string? accountNumber { get; set; }
        public string? subAccount { get; set; }
        public string? content { get; set; }
        public string? transferType { get; set; } // "in" or "out"
        public decimal transferAmount { get; set; }
        public decimal accumulated { get; set; }
        public string? channel { get; set; }
        public string? referenceCode { get; set; }
    }
}
