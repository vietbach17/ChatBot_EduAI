using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages
{
    /// <summary>
    /// PageModel trang Chinh sach Bao mat. Hien thi noi dung dieu khoan va chinh sach su dung du lieu cua he thong.
    /// </summary>
    public class PrivacyModel : PageModel
    {
        private readonly ILogger<PrivacyModel> _logger;

        public PrivacyModel(ILogger<PrivacyModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }

}
