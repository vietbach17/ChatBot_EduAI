namespace BussinessLayer.DTOs
{
    public class PaymentRequest
    {
        public int TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string OrderDescription { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string IpAddress { get; set; } = "127.0.0.1";
    }
}
