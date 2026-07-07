using System.ComponentModel.DataAnnotations;

namespace PresentationLayer.ViewModels.Admin
{
    public class NotificationSendViewModel
    {
        public string TargetAudience { get; set; } = "All";

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề email")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung email")]
        public string EmailContent { get; set; } = string.Empty;
    }
}
