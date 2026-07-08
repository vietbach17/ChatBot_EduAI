using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace PresentationLayer.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ISubjectService _subjectService;

        public IndexModel(ILogger<IndexModel> logger, ISubscriptionService subscriptionService, ISubjectService subjectService)
        {
            _logger = logger;
            _subscriptionService = subscriptionService;
            _subjectService = subjectService;
        }

        public SubscriptionInfoDto? SubscriptionInfo { get; set; }
        public int TotalSubjects { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (role == "Admin") return RedirectToPage("/Admin/Dashboard");
                if (role == "Lecturer") return RedirectToPage("/Lecturer/MySubjects");
                
                // Load data for Student Overview
                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    SubscriptionInfo = await _subscriptionService.GetSubscriptionInfoAsync(userId);
                }
                
                var subjects = await _subjectService.GetAllSubjectsAsync();
                TotalSubjects = subjects.Count();

                return Page();
            }
            return RedirectToPage("/Auth/Login");
        }
    }
}
