namespace BussinessLayer.DTOs
{
    /// <summary>
    /// Cấu hình chia nhỏ (chunking) nội dung tài liệu khi tạo Embedding AI.
    /// </summary>
    public class ChunkSettingsDto
    {
        /// <summary>Số từ tối đa trong một chunk.</summary>
        public int MaxWords { get; set; } = 300;

        /// <summary>Số từ chồng lấn (overlap) giữa hai chunk liên tiếp để giữ mạch ngữ cảnh.</summary>
        public int OverlapWords { get; set; } = 50;
    }
}
