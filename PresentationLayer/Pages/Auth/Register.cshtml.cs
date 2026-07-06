using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.ViewModels.Auth;

namespace PresentationLayer.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly IAuthService _authService;

        public RegisterModel(IAuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public RegisterViewModel Input { get; set; } = new RegisterViewModel();

        public void OnGet()
        {
            // Nếu đã đăng nhập thì chuyển về trang chủ
            if (User.Identity?.IsAuthenticated == true)
            {
                Response.Redirect("/");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var dto = new RegisterDto
            {
                Username = Input.Username,
                Password = Input.Password,
                Email = Input.Email
            };

            var (success, error) = await _authService.RegisterAsync(dto);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error);
                return Page();
            }

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToPage("/Auth/Login");
        }
    }
}
