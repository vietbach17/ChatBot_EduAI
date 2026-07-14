using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Report
{
    [Authorize(Roles = "Student,Lecturer")]
    /// <summary>
    /// PageModel trang Báo cáo / Khiếu nại của Sinh viên và Giảng viên.
    /// Cho phép gửi báo cáo mới (kèm ảnh) và xem lại các báo cáo đã gửi cùng phản hồi từ Admin.
    /// </summary>
    public class MyReportsModel : PageModel
    {
        private readonly IReportService _reportService;

        private static readonly string[] AllowedImageExt = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxImageBytes = 5 * 1024 * 1024; // 5MB

        public MyReportsModel(IReportService reportService)
        {
            _reportService = reportService;
        }

        [BindProperty]
        public CreateReportInputModel Input { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public List<ReportDto> MyReports { get; set; } = new();
        public Dictionary<string, string> Categories { get; set; } = new();
        public string CurrentRole { get; set; } = "Student";

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? transactionId, string? category)
        {
            var userId = GetUserId();
            if (userId == 0) return RedirectToPage("/Auth/Login");

            CurrentRole = GetRole();
            Categories = ReportCategories.ForRole(CurrentRole);

            // Cho phép mở sẵn form báo cáo giao dịch từ trang Lịch sử thanh toán
            if (transactionId.HasValue) Input.RelatedTransactionId = transactionId;
            if (!string.IsNullOrEmpty(category) && Categories.ContainsKey(category)) Input.Category = category;

            MyReports = (await _reportService.GetMyReportsAsync(userId)).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = GetUserId();
            if (userId == 0) return RedirectToPage("/Auth/Login");

            CurrentRole = GetRole();
            Categories = ReportCategories.ForRole(CurrentRole);

            // Validate
            if (string.IsNullOrWhiteSpace(Input.Title) || string.IsNullOrWhiteSpace(Input.Description) ||
                string.IsNullOrWhiteSpace(Input.Category) || !Categories.ContainsKey(Input.Category))
            {
                StatusMessage = "Error: Vui lòng nhập đầy đủ tiêu đề, mô tả và chọn loại vấn đề hợp lệ.";
                MyReports = (await _reportService.GetMyReportsAsync(userId)).ToList();
                return Page();
            }

            string? imageUrl = null;
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var ext = Path.GetExtension(ImageFile.FileName).ToLowerInvariant();
                if (!AllowedImageExt.Contains(ext))
                {
                    StatusMessage = "Error: Ảnh phải có định dạng JPG, PNG, GIF hoặc WEBP.";
                    MyReports = (await _reportService.GetMyReportsAsync(userId)).ToList();
                    return Page();
                }
                if (ImageFile.Length > MaxImageBytes)
                {
                    StatusMessage = "Error: Ảnh vượt quá dung lượng cho phép (tối đa 5MB).";
                    MyReports = (await _reportService.GetMyReportsAsync(userId)).ToList();
                    return Page();
                }

                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reports");
                Directory.CreateDirectory(uploadsDir);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }
                imageUrl = $"/uploads/reports/{fileName}";
            }

            await _reportService.CreateReportAsync(new CreateReportDto
            {
                ReporterId = userId,
                ReporterRole = CurrentRole,
                Category = Input.Category,
                Title = Input.Title,
                Description = Input.Description,
                ImageUrl = imageUrl,
                RelatedTransactionId = Input.RelatedTransactionId
            });

            StatusMessage = "Thành công: Đã gửi báo cáo. Admin sẽ xem xét và phản hồi sớm nhất.";
            return RedirectToPage();
        }

        private int GetUserId()
        {
            var val = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value
                   ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : 0;
        }

        private string GetRole()
            => User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value == "Lecturer" ? "Lecturer" : "Student";

        /// <summary>Dữ liệu nhập từ form gửi báo cáo: loại vấn đề, tiêu đề, mô tả và mã giao dịch liên quan.</summary>
        public class CreateReportInputModel
        {
            public string Category { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int? RelatedTransactionId { get; set; }
        }
    }
}
