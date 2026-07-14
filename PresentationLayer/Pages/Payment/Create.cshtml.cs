using BussinessLayer.IServices;
using BussinessLayer.IGateways;
using BussinessLayer.Gateways;
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
    /// <summary>
    /// PageModel trang Tao don Thanh toan. Hien thi chi tiet goi duoc chon va chuyen huong den cong thanh toan phu hop.
    /// </summary>
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

        public SubscriptionPlanDto? Plan { get; set; }
        public AddonPackageDto? Addon { get; set; }

        public async Task<IActionResult> OnGetAsync(string? plan, int? addonId)
        {
            if (string.IsNullOrEmpty(plan) && !addonId.HasValue)
            {
                return RedirectToPage("/Subscription/Index");
            }

            if (addonId.HasValue)
            {
                var targetAddon = await _planService.GetAddonByIdAsync(addonId.Value);
                if (targetAddon == null || !targetAddon.IsActive || targetAddon.Price <= 0)
                {
                    return RedirectToPage("/Subscription/Index");
                }
                Addon = targetAddon;
                return Page();
            }

            if (!string.IsNullOrEmpty(plan))
            {
                var allPlans = await _planService.GetAllAsync();
                var targetPlan = allPlans.FirstOrDefault(p => p.Name.Equals(plan, StringComparison.OrdinalIgnoreCase));

                if (targetPlan == null || targetPlan.Price <= 0)
                {
                    return RedirectToPage("/Subscription/Index");
                }

                Plan = targetPlan;
                return Page();
            }

            return RedirectToPage("/Subscription/Index");
        }

        public async Task<IActionResult> OnPostAsync(int? planId, int? addonId, string paymentMethod)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return RedirectToPage("/Auth/Login");
            }

            if (!planId.HasValue && !addonId.HasValue)
            {
                TempData["ErrorMessage"] = "Yêu cầu không hợp lệ.";
                return RedirectToPage("/Subscription/Index");
            }

            try
            {
                PaymentTransactionDto transaction;
                string orderDesc;

                if (addonId.HasValue)
                {
                    transaction = await _paymentService.CreateAddonTransactionAsync(userId, addonId.Value, paymentMethod);
                    var addon = await _planService.GetAddonByIdAsync(addonId.Value);
                    orderDesc = $"Thanh toan mua goi nap them {addon?.Name} cho account {User.Identity?.Name}";
                }
                else
                {
                    var plan = await _planService.GetByIdAsync(planId.Value);
                    if (plan == null || plan.Price <= 0)
                    {
                        TempData["ErrorMessage"] = "Gói cước không hợp lệ.";
                        return RedirectToPage("/Subscription/Index");
                    }
                    transaction = await _paymentService.CreateTransactionAsync(userId, planId.Value, paymentMethod);
                    orderDesc = $"Thanh toan mua goi {plan.Name} cho account {User.Identity?.Name}";
                }

                var gateway = _gatewayFactory.GetGateway(paymentMethod);
                
                // Dynamically construct return callback URL based on selected gateway
                string callbackPage = paymentMethod.Equals("VNPay", StringComparison.OrdinalIgnoreCase) 
                    ? "/Payment/VNPayCallback" 
                    : "/PaymentHistory/Index"; // Fallback for SePay/others to history page

                var returnUrl = $"{Request.Scheme}://{Request.Host}{callbackPage}";

                var paymentRequest = new PaymentRequest
                {
                    TransactionId = transaction.Id,
                    Amount = transaction.Amount,
                    OrderDescription = orderDesc,
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
