namespace PresentationLayer.ViewModels.Admin
{
    /// <summary>ViewModel tạo mới người dùng: tên đăng nhập, mật khẩu, vai trò và email.</summary>
    public class UserCreateViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";
        public string? Email { get; set; }
    }

    /// <summary>ViewModel cập nhật người dùng (mật khẩu để trống nếu không đổi).</summary>
    public class UserUpdateViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}
