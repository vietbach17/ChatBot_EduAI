namespace PresentationLayer.ViewModels.Admin
{
    public class UserCreateViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";
        public string? Email { get; set; }
    }

    public class UserUpdateViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}
