namespace PresentationLayer.ViewModels.Lecturer
{
    /// <summary>ViewModel giảng viên cập nhật thông tin môn học của mình.</summary>
    public class SubjectUpdateViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
