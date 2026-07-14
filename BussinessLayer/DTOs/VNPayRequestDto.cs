using System;

namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO yêu cầu tạo URL thanh toán VNPay.
    /// </summary>
    public class VNPayRequestDto
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string OrderDescription { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
    }
}
