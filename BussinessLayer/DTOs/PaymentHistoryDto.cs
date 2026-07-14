using System;

namespace BussinessLayer.DTOs
{
    public class PaymentHistoryDto
    {
        public int TransactionId { get; set; }
        public string TransactionCode { get; set; }
        public string UserName { get; set; }
        public string PlanName { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } // VNPay, PayOS, SePay
        public string Status { get; set; } // Pending, Success, Failed
        public string Classification { get; set; } // "Gói Hội Viên" or "Lượt Hỏi Dự Phòng"
        public DateTime Date { get; set; }
    }
}
