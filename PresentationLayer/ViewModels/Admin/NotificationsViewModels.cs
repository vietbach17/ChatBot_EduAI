using System.ComponentModel.DataAnnotations;

namespace PresentationLayer.ViewModels.Admin
{
    /// <summary>ViewModel gửi thông báo qua email: đối tượng nhận, tiêu đề và nội dung.</summary>
    public class NotificationSendViewModel
    {
        public string TargetAudience { get; set; } = "All";

        public string SpecificEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề email")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung email")]
        public string EmailContent { get; set; } = string.Empty;
    }
}
