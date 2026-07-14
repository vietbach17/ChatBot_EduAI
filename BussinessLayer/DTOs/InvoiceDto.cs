using System;

namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO chi tiết Hóa đơn thanh toán gửi qua Email.
    /// </summary>
    public class InvoiceDto
    {
        public int TransactionId { get; set; }
        public string TransactionCode { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string PlanName { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public string Classification { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
