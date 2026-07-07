using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using PresentationLayer.ViewModels.Admin;

namespace PresentationLayer.Pages.Admin
{
    public class UsersModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;

        public UsersModel(IUserService userService, IEmailService emailService)
        {
            _userService = userService;
            _emailService = emailService;
        }

        public IEnumerable<UserDto> Users { get; set; } = new List<UserDto>();

        [BindProperty] public UserCreateViewModel CreateModel { get; set; } = new UserCreateViewModel();
        [BindProperty] public UserUpdateViewModel UpdateModel { get; set; } = new UserUpdateViewModel();

        public async Task OnGetAsync()
        {
            Users = await _userService.GetAllUsersAsync(false);
        }

        public async Task<IActionResult> OnPostAddUserAsync()
        {
            if (!string.IsNullOrWhiteSpace(CreateModel.Username) && !string.IsNullOrWhiteSpace(CreateModel.Password))
            {
                var created = await _userService.CreateUserAsync(CreateModel.Username, CreateModel.Password, CreateModel.Role, CreateModel.Email);

                // Gửi email thông tin tài khoản nếu có email
                if (created && !string.IsNullOrWhiteSpace(CreateModel.Email))
                {
                    try
                    {
                        await _emailService.SendAccountCreatedEmailAsync(CreateModel.Email, CreateModel.Username, CreateModel.Password, CreateModel.Role);
                        TempData["SuccessMessage"] = $"Tạo tài khoản thành công! Đã gửi thông tin đến {CreateModel.Email}.";
                    }
                    catch
                    {
                        TempData["WarnMessage"] = "Tạo tài khoản thành công nhưng không gửi được email. Kiểm tra lại cấu hình SMTP.";
                    }
                }
                else if (created)
                {
                    TempData["SuccessMessage"] = "Tạo tài khoản thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Tên đăng nhập đã tồn tại.";
                }
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateUserAsync()
        {
            if (UpdateModel.Id > 0 && !string.IsNullOrWhiteSpace(UpdateModel.Username))
            {
                await _userService.UpdateUserAsync(UpdateModel.Id, UpdateModel.Username, UpdateModel.Role, UpdateModel.Email, UpdateModel.Password);
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(int id)
        {
            await _userService.SoftDeleteUserAsync(id);
            return RedirectToPage();
        }
    }
}
