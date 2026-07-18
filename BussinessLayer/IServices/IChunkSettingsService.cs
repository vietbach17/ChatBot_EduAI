using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Quản lý cấu hình chunk file (kích thước chunk, số từ chồng lấn) dùng khi xử lý tài liệu.
    /// </summary>
    public interface IChunkSettingsService
    {
        /// <summary>Lấy cấu hình chunk hiện tại.</summary>
        ChunkSettingsDto GetSettings();

        /// <summary>Cập nhật cấu hình chunk. Trả về (thành công, thông báo lỗi nếu có).</summary>
        Task<(bool Success, string? Error)> UpdateAsync(ChunkSettingsDto settings);

        /// <summary>Khôi phục cấu hình chunk về giá trị mặc định.</summary>
        Task<(bool Success, string? Error)> ResetToDefaultAsync();
    }
}
