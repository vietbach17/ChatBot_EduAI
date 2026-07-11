using BussinessLayer.IServices;
using BussinessLayer.IGateways;
using BussinessLayer.Gateways;
using System;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.SignalR;

namespace PresentationLayer.Pages.Payment
{
    [Authorize]
    public class VNPayCallbackModel : PageModel
    {
        private readonly PaymentGatewayFactory _gatewayFactory;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IPaymentService _paymentService;
        private readonly IHubContext<SignalRHub> _hubContext;

        public VNPayCallbackModel(
            PaymentGatewayFactory gatewayFactory,
            ISubscriptionService subscriptionService,
            IPaymentService paymentService,
            IHubContext<SignalRHub> hubContext)
        {
            _gatewayFactory = gatewayFactory;
            _subscriptionService = subscriptionService;
            _paymentService = paymentService;
            _hubContext = hubContext;
        }

        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public PaymentTransactionDto Transaction { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            var queryDict = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());

            if (!queryDict.Any())
            {
                return RedirectToPage("/Subscription/Index");
            }

            try
            {
                var gateway = _gatewayFactory.GetGateway("VNPay");
                bool isValidSignature = await gateway.ValidateCallback(queryDict);

                if (!isValidSignature)
                {
                    IsSuccess = false;
                    Message = "Chữ ký giao dịch không hợp lệ hoặc đã bị thay đổi.";
                    return Page();
                }

                var txnRef = Request.Query["vnp_TxnRef"].ToString();
                var parts = txnRef.Split('_');
                if (parts.Length == 0 || !int.TryParse(parts[0], out int transactionId))
                {
                    IsSuccess = false;
                    Message = "Mã giao dịch không đúng định dạng.";
                    return Page();
                }

                var transaction = await _paymentService.GetTransactionByIdAsync(transactionId);
                if (transaction == null)
                {
                    IsSuccess = false;
                    Message = "Không tìm thấy thông tin giao dịch trong hệ thống.";
                    return Page();
                }

                Transaction = transaction;

                var responseCode = Request.Query["vnp_ResponseCode"].ToString();
                var transactionNo = Request.Query["vnp_TransactionNo"].ToString();

                if (responseCode == "00")
                {
                    // Success! Process payment success
                    var processed = await _subscriptionService.ProcessPaymentSuccessAsync(transactionId, transactionNo);
                    if (processed)
                    {
                        await _hubContext.Clients.All.SendAsync("PaymentStatusUpdated", transactionId, "Success");
                        IsSuccess = true;
                        Message = $"Thanh toán thành công gói {transaction.PlanName}!";
                    }
                    else
                    {
                        IsSuccess = false;
                        Message = "Thanh toán thành công nhưng không thể kích hoạt gói. Vui lòng liên hệ Admin.";
                    }
                }
                else
                {
                    // Failed
                    await _paymentService.UpdateTransactionStatusAsync(transactionId, "Failed", transactionNo);

                    IsSuccess = false;
                    Message = GetVNPayErrorMessage(responseCode);
                }

                // Reload transaction to get updated status and code
                var updatedTx = await _paymentService.GetTransactionByIdAsync(transactionId);
                if (updatedTx != null)
                {
                    Transaction = updatedTx;
                }
            }
            catch (Exception ex)
            {
                IsSuccess = false;
                Message = $"Lỗi xử lý phản hồi từ VNPay: {ex.Message}";
            }

            return Page();
        }

        private string GetVNPayErrorMessage(string responseCode)
        {
            return responseCode switch
            {
                "07" => "Trừ tiền thành công nhưng giao dịch bị nghi ngờ (phân loại giao dịch gian lận, bất thường).",
                "09" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking tại ngân hàng.",
                "10" => "Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần.",
                "11" => "Giao dịch không thành công do: Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch.",
                "12" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng bị khóa.",
                "24" => "Khách hàng hủy giao dịch thanh toán.",
                "51" => "Giao dịch không thành công do: Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.",
                "65" => "Giao dịch không thành công do: Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.",
                "75" => "Ngân hàng thanh toán đang bảo trì.",
                "79" => "Giao dịch không thành công do: Khách hàng nhập sai mật khẩu thanh toán quá số lần quy định.",
                _ => "Giao dịch thất bại hoặc xảy ra lỗi trong quá trình thanh toán."
            };
        }
    }
}

