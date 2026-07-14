using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace PresentationLayer.Pages.Auth
{
    /// <summary>
    /// PageModel xu ly dang xuat. Xoa cookie xac thuc va chuyen huong nguoi dung ve trang dang nhap.
    /// </summary>
    public class LogoutModel : PageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Auth/Login");
        }
    }
}
