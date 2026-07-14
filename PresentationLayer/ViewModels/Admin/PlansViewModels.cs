namespace PresentationLayer.ViewModels.Admin
{
    /// <summary>ViewModel tạo mới gói đăng ký: tên, mô tả, giá, giới hạn câu hỏi và thứ tự hiển thị.</summary>
    public class PlanCreateViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Limit { get; set; } = 5;
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>ViewModel cập nhật gói đăng ký hiện có.</summary>
    public class PlanUpdateViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Limit { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
