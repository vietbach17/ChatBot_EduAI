using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace PresentationLayer.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/Auth/Profile");
            }
            return RedirectToPage("/Auth/Login");
        }
    }
}
