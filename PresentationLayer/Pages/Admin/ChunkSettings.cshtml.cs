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
    /// PageModel trang Cấu hình Chunk File của Admin. Cho phép admin điều chỉnh số từ tối đa mỗi chunk
    /// và số từ chồng lấn giữa các chunk khi xử lý tài liệu tạo Embedding AI.
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

        public int DefaultMaxWords => ChunkSettingsService.DefaultMaxWords;
        public int DefaultOverlapWords => ChunkSettingsService.DefaultOverlapWords;
        public int MinMaxWords => ChunkSettingsService.MinMaxWords;
        public int MaxMaxWordsLimit => ChunkSettingsService.MaxMaxWords;
        public int MaxOverlapWordsLimit => ChunkSettingsService.MaxOverlapWords;

        public void OnGet()
        {
            var settings = _chunkSettingsService.GetSettings();
            MaxWords = settings.MaxWords;
            OverlapWords = settings.OverlapWords;
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            var (ok, err) = await _chunkSettingsService.UpdateAsync(new ChunkSettingsDto
            {
                MaxWords = MaxWords,
                OverlapWords = OverlapWords
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
