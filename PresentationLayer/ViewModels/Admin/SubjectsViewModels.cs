namespace PresentationLayer.ViewModels.Admin
{
    /// <summary>ViewModel tạo mới môn học: mã môn, tên môn và giảng viên phụ trách.</summary>
    public class SubjectCreateViewModel
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? LecturerId { get; set; }
    }

    /// <summary>ViewModel cập nhật thông tin môn học hiện có.</summary>
    public class SubjectUpdateViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? LecturerId { get; set; }
    }
}
