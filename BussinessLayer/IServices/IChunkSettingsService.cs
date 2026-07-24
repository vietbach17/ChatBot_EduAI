using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Quản lý chính sách chunk file do Admin thiết lập: template mặc định toàn hệ thống
    /// và khoảng giá trị Giảng viên được phép tự cấu hình cho môn mình phụ trách.
    /// </summary>
    public interface IChunkSettingsService
    {
        /// <summary>Lấy template chunk mặc định (áp dụng cho môn không có cấu hình riêng).</summary>
        ChunkSettingsDto GetSettings();

        /// <summary>Lấy toàn bộ chính sách chunk hiện tại (template + quyền và khoảng cho Giảng viên).</summary>
        ChunkPolicyDto GetPolicy();

        /// <summary>Cập nhật chính sách chunk. Trả về (thành công, thông báo lỗi nếu có).</summary>
        Task<(bool Success, string? Error)> UpdateAsync(ChunkPolicyDto policy);

        /// <summary>Khôi phục chính sách chunk về giá trị mặc định.</summary>
        Task<(bool Success, string? Error)> ResetToDefaultAsync();

        /// <summary>
        /// Kiểm tra cấu hình riêng của Giảng viên có nằm trong khoảng Admin cho phép không.
        /// Trả về null nếu hợp lệ, ngược lại là thông báo lỗi.
        /// </summary>
        string? ValidateLecturerSettings(ChunkSettingsDto settings);

        /// <summary>
        /// Kiểm tra số từ tối đa mỗi chunk mà Giảng viên tự đặt có nằm trong khoảng Admin cho phép không.
        /// Truyền null nghĩa là Giảng viên dùng template của Admin (luôn hợp lệ).
        /// Trả về null nếu hợp lệ, ngược lại là thông báo lỗi.
        /// </summary>
        string? ValidateLecturerMaxWords(int? maxWords);
    }
}
