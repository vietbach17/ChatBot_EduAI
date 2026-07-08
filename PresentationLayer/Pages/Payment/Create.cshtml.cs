using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Payment
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ISubscriptionPlanService _planService;
        private readonly IPaymentService _paymentService;
        private readonly PaymentGatewayFactory _gatewayFactory;

        public CreateModel(
            ISubscriptionPlanService planService,
            IPaymentService paymentService,
            PaymentGatewayFactory gatewayFactory)
        {
            _planService = planService;
            _paymentService = paymentService;
            _gatewayFactory = gatewayFactory;
        }

        public SubscriptionPlanDto Plan { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(string plan)
        {
            if (string.IsNullOrEmpty(plan))
            {
                return RedirectToPage("/Subscription/Index");
            }

            var allPlans = await _planService.GetAllAsync();
            var targetPlan = allPlans.FirstOrDefault(p => p.Name.Equals(plan, StringComparison.OrdinalIgnoreCase));

            if (targetPlan == null || targetPlan.Price <= 0)
            {
                return RedirectToPage("/Subscription/Index");
            }

            Plan = targetPlan;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int planId, string paymentMethod)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return RedirectToPage("/Auth/Login");
            }

            var plan = await _planService.GetByIdAsync(planId);
            if (plan == null || plan.Price <= 0)
            {
                TempData["ErrorMessage"] = "Gói cước không hợp lệ.";
                return RedirectToPage("/Subscription/Index");
            }

            try
            {
                // Call PaymentService instead of calling repository directly
                var transaction = await _paymentService.CreateTransactionAsync(userId, planId, paymentMethod);

                var gateway = _gatewayFactory.GetGateway(paymentMethod);
                
                // Dynamically construct return callback URL based on selected gateway
                string callbackPage = paymentMethod.Equals("VNPay", StringComparison.OrdinalIgnoreCase) 
                    ? "/Payment/VNPayCallback" 
                    : "/Payment/VNPayCallback"; // Fallback/Default

                var returnUrl = $"{Request.Scheme}://{Request.Host}{callbackPage}";

                var paymentRequest = new PaymentRequest
                {
                    TransactionId = transaction.Id,
                    Amount = transaction.Amount,
                    OrderDescription = $"Thanh toan mua goi {plan.Name} cho account {User.Identity?.Name}",
                    ReturnUrl = returnUrl,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
                };

                var paymentUrl = await gateway.CreatePaymentUrl(paymentRequest);
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi khởi tạo thanh toán: {ex.Message}";
                return RedirectToPage("/Subscription/Index");
            }
        }

        private int GetUserId()
        {
            var val = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value
                   ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : 0;
        }
    }
}
