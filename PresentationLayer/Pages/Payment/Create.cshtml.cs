using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Services;
using BussinessLayer.DTOs;
using BussinessLayer.Helpers;

namespace PresentationLayer.Pages.Payment
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly IVNPayService _vnPayService;

        public CreateModel(IVNPayService vnPayService)
        {
            _vnPayService = vnPayService;
        }

        public IActionResult OnGet(string plan)
        {
            if (string.IsNullOrEmpty(plan))
            {
                return RedirectToPage("/Subscription/Index");
            }

            var val = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value
                   ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(val, out var userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Determine amount based on plan (can also be queried from DB if we fetch from SubscriptionPlanService)
            decimal amount = 0;
            if (plan == "Basic") amount = 50000;
            else if (plan == "Premium") amount = 150000;
            else return RedirectToPage("/Subscription/Index");

            var requestDto = new VNPayRequestDto
            {
                UserId = userId,
                Amount = amount,
                PlanName = plan,
                OrderDescription = $"Thanh toan nang cap goi {plan}",
                IpAddress = GetIpAddress(),
                ReturnUrl = Url.Page("/Payment/Callback", null, null, Request.Scheme) ?? string.Empty
            };

            string paymentUrl = _vnPayService.CreatePaymentUrl(requestDto);
            return Redirect(paymentUrl);
        }

        private string GetIpAddress()
        {
            var ipAddress = string.Empty;
            try
            {
                var remoteIpAddress = HttpContext.Connection.RemoteIpAddress;
                
                if (remoteIpAddress != null)
                {
                    if (remoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        remoteIpAddress = System.Net.Dns.GetHostEntry(remoteIpAddress).AddressList
                            .FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    }

                    if (remoteIpAddress != null) ipAddress = remoteIpAddress.ToString();

                    if (ipAddress == "127.0.0.1" || ipAddress == "::1")
                    {
                        ipAddress = "127.0.0.1";
                    }
                }
            }
            catch (System.Exception)
            {
                ipAddress = "127.0.0.1";
            }

            return string.IsNullOrEmpty(ipAddress) ? "127.0.0.1" : ipAddress;
        }
    }
}
