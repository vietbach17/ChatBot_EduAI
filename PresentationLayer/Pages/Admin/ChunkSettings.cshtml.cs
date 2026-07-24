using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using BussinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    /// <summary>
    /// PageModel trang Cấu hình Chunk File của Admin. Admin đặt template mặc định (số từ tối đa mỗi chunk,
    /// số từ chồng lấn) cho toàn hệ thống, đồng thời quyết định Giảng viên có được tự cấu hình chunk
    /// cho môn mình phụ trách hay không và trong khoảng giá trị nào.
    /// </summary>
    public class ChunkSettingsModel : PageModel
    {
        private readonly IChunkSettingsService _chunkSettingsService;

        public ChunkSettingsModel(IChunkSettingsService chunkSettingsService)
        {
            _chunkSettingsService = chunkSettingsService;
        }

        [BindProperty] public int MaxWords { get; set; }
        [BindProperty] public int OverlapWords { get; set; }
        [BindProperty] public int LecturerMinWords { get; set; }
        [BindProperty] public int LecturerMaxWords { get; set; }


        public int DefaultMaxWords => ChunkSettingsService.DefaultMaxWords;
        public int DefaultOverlapWords => ChunkSettingsService.DefaultOverlapWords;
        public int MinMaxWords => ChunkSettingsService.MinMaxWords;
        public int MaxMaxWordsLimit => ChunkSettingsService.MaxMaxWords;
        public int MaxOverlapWordsLimit => ChunkSettingsService.MaxOverlapWords;
        public int DefaultLecturerMinWords => ChunkSettingsService.DefaultLecturerMinWords;
        public int DefaultLecturerMaxWords => ChunkSettingsService.DefaultLecturerMaxWords;

        public void OnGet()
        {
            var policy = _chunkSettingsService.GetPolicy();
            MaxWords = policy.MaxWords;
            OverlapWords = policy.OverlapWords;
            LecturerMinWords = policy.LecturerMinWords;
            LecturerMaxWords = policy.LecturerMaxWords;
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            var current = _chunkSettingsService.GetPolicy();

            // Trần chồng lấn của Giảng viên không có trên UI, nhưng chính sách yêu cầu nó phải nhỏ hơn
            // trần số từ mỗi chunk — kẹp lại theo giới hạn Admin vừa đặt để không rơi vào lỗi khó hiểu.
            var lecturerMaxOverlap = System.Math.Max(0, System.Math.Min(current.LecturerMaxOverlapWords, LecturerMaxWords - 1));

            var (ok, err) = await _chunkSettingsService.UpdateAsync(new ChunkPolicyDto
            {
                MaxWords = MaxWords,
                OverlapWords = OverlapWords,
                AllowLecturerOverride = current.AllowLecturerOverride,
                LecturerMinWords = LecturerMinWords,
                LecturerMaxWords = LecturerMaxWords,
                LecturerMaxOverlapWords = lecturerMaxOverlap
            });

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Đã lưu cấu hình chunk!" : err;
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostResetAsync()
        {
            var (ok, err) = await _chunkSettingsService.ResetToDefaultAsync();
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? $"Đã khôi phục mặc định ({ChunkSettingsService.DefaultMaxWords} từ/chunk, chồng lấn {ChunkSettingsService.DefaultOverlapWords} từ)!"
                : err;
            return RedirectToPage();
        }
    }
}
