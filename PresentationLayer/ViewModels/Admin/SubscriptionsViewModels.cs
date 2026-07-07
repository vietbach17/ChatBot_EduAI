namespace PresentationLayer.ViewModels.Admin
{
    public class SubscriptionUpdateViewModel
    {
        public int UserId { get; set; }
        public string PlanValue { get; set; } = "Free";
        public string? Expiry { get; set; }
    }
}
