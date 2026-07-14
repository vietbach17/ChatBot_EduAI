namespace PresentationLayer.ViewModels.Admin
{
    /// <summary>ViewModel admin cập nhật gói đăng ký của người dùng và ngày hết hạn.</summary>
    public class SubscriptionUpdateViewModel
    {
        public int UserId { get; set; }
        public string PlanValue { get; set; } = "Free";
        public string? Expiry { get; set; }
    }
}
