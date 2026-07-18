using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using PresentationLayer.ViewModels.Admin;

namespace PresentationLayer.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    /// <summary>
    /// PageModel trang Thong bao cua Admin. Hien thi danh sach thong bao he thong va cho phep admin xoa hoac danh dau da doc.
    /// </summary>
    public class NotificationsModel : PageModel
    {
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;

        public NotificationsModel(IEmailService emailService, IUserService userService)
        {
            _emailService = emailService;
            _userService = userService;
        }

        [BindProperty]
        public NotificationSendViewModel SendModel { get; set; } = new NotificationSendViewModel();

        public void OnGet()
        {
            ViewData["ActiveMenu"] = "AdminDashboard";
        }

        public async Task<IActionResult> OnPostSendAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var allUsers = await _userService.GetAllUsersAsync();
            var validEmails = new System.Collections.Generic.List<string>();

            if (SendModel.TargetAudience == "Specific")
            {
                if (string.IsNullOrWhiteSpace(SendModel.SpecificEmail))
                {
                    ModelState.AddModelError("SendModel.SpecificEmail", "Vui lòng nhập email của tài khoản cụ thể.");
                    return Page();
                }

                var targetUser = allUsers.FirstOrDefault(u => u.Email != null && u.Email.Equals(SendModel.SpecificEmail, System.StringComparison.OrdinalIgnoreCase));
                if (targetUser == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng nào có địa chỉ email này trong hệ thống.";
                    return RedirectToPage();
                }

                validEmails.Add(targetUser.Email!);
            }
            else
            {
                var targetUsers = SendModel.TargetAudience switch
                {
                    "Students" => allUsers.Where(u => u.Role == "Student"),
                    "Lecturers" => allUsers.Where(u => u.Role == "Lecturer"),
                    _ => allUsers
                };

                validEmails = targetUsers
                    .Where(u => !string.IsNullOrWhiteSpace(u.Email))
                    .Select(u => u.Email!)
                    .Distinct()
                    .ToList();
            }

            if (!validEmails.Any())
            {
                TempData["ErrorMessage"] = "Không tìm thấy địa chỉ email hợp lệ nào cho nhóm đối tượng này.";
                return RedirectToPage();
            }

            await _emailService.SendBroadcastEmailAsync(validEmails, SendModel.Subject, SendModel.EmailContent);

            TempData["SuccessMessage"] = $"Đã gửi thông báo thành công đến {validEmails.Count} email.";
            return RedirectToPage();
        }
    }
}
