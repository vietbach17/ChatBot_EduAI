namespace BussinessLayer.DTOs
{
    /// <summary>
    /// Cấu hình chia nhỏ (chunking) nội dung tài liệu khi tạo Embedding AI.
    /// Đây là giá trị "hiệu lực" thực sự dùng khi xử lý một tài liệu.
    /// </summary>
    public class ChunkSettingsDto
    {
        /// <summary>Số từ tối đa trong một chunk.</summary>
        public int MaxWords { get; set; } = 300;

        /// <summary>Số từ chồng lấn (overlap) giữa hai chunk liên tiếp để giữ mạch ngữ cảnh.</summary>
        public int OverlapWords { get; set; } = 50;
    }

    /// <summary>
    /// Chính sách chunk do Admin thiết lập: giá trị template mặc định cho toàn hệ thống
    /// và khoảng giá trị mà Giảng viên được phép tự cấu hình cho môn mình phụ trách.
    /// </summary>
    public class ChunkPolicyDto
    {
        /// <summary>Số từ tối đa mỗi chunk của template mặc định.</summary>
        public int MaxWords { get; set; } = 300;

        /// <summary>Số từ chồng lấn của template mặc định.</summary>
        public int OverlapWords { get; set; } = 50;

        /// <summary>Cho phép Giảng viên tự cấu hình chunk riêng cho môn mình phụ trách.</summary>
        public bool AllowLecturerOverride { get; set; } = true;

        /// <summary>Số từ tối đa mỗi chunk nhỏ nhất mà Giảng viên được đặt.</summary>
        public int LecturerMinWords { get; set; } = 100;

        /// <summary>Số từ tối đa mỗi chunk lớn nhất mà Giảng viên được đặt.</summary>
        public int LecturerMaxWords { get; set; } = 800;

        /// <summary>Số từ chồng lấn lớn nhất mà Giảng viên được đặt.</summary>
        public int LecturerMaxOverlapWords { get; set; } = 300;

        /// <summary>Tách phần template mặc định ra khỏi chính sách.</summary>
        public ChunkSettingsDto ToSettings() => new ChunkSettingsDto
        {
            MaxWords = MaxWords,
            OverlapWords = OverlapWords
        };
    }

    /// <summary>
    /// Cấu hình chunk của một Môn học kèm ngữ cảnh chính sách, dùng để hiển thị/chỉnh sửa
    /// trên trang Quản lý Môn học của Giảng viên.
    /// </summary>
    public class SubjectChunkSettingsDto
    {
        public int SubjectId { get; set; }

        /// <summary>True nếu môn đang dùng cấu hình riêng; False nếu đang theo template của Admin.</summary>
        public bool UseCustom { get; set; }

        /// <summary>Số từ tối đa mỗi chunk đang có hiệu lực cho môn này.</summary>
        public int MaxWords { get; set; }

        /// <summary>Số từ chồng lấn đang có hiệu lực cho môn này.</summary>
        public int OverlapWords { get; set; }

        /// <summary>Chính sách hiện hành của Admin (template mặc định + khoảng cho phép).</summary>
        public ChunkPolicyDto Policy { get; set; } = new ChunkPolicyDto();
    }
}
