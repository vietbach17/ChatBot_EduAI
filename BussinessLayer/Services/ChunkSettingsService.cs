using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Dịch vụ Cấu hình Chunk File. Lưu cấu hình (số từ tối đa mỗi chunk, số từ chồng lấn)
    /// vào file JSON để giữ giá trị qua các lần khởi động lại app. Đăng ký dạng Singleton.
    /// </summary>
    public class ChunkSettingsService : IChunkSettingsService
    {
        public const int DefaultMaxWords = 300;
        public const int DefaultOverlapWords = 50;
        public const int MinMaxWords = 50;
        public const int MaxMaxWords = 2000;
        public const int MaxOverlapWords = 500;

        private readonly string _filePath;
        private readonly object _lock = new object();
        private ChunkSettingsDto _current;

        public ChunkSettingsService(string filePath)
        {
            _filePath = filePath;
            _current = LoadFromFile();
        }

        public ChunkSettingsDto GetSettings()
        {
            lock (_lock)
            {
                return new ChunkSettingsDto
                {
                    MaxWords = _current.MaxWords,
                    OverlapWords = _current.OverlapWords
                };
            }
        }

        public Task<(bool Success, string? Error)> UpdateAsync(ChunkSettingsDto settings)
        {
            var error = Validate(settings);
            if (error != null)
            {
                return Task.FromResult<(bool, string?)>((false, error));
            }

            lock (_lock)
            {
                try
                {
                    var dir = Path.GetDirectoryName(_filePath);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_filePath, json);
                    _current = new ChunkSettingsDto
                    {
                        MaxWords = settings.MaxWords,
                        OverlapWords = settings.OverlapWords
                    };
                    return Task.FromResult<(bool, string?)>((true, null));
                }
                catch (Exception ex)
                {
                    return Task.FromResult<(bool, string?)>((false, $"Không thể lưu cấu hình: {ex.Message}"));
                }
            }
        }

        public Task<(bool Success, string? Error)> ResetToDefaultAsync()
        {
            return UpdateAsync(new ChunkSettingsDto
            {
                MaxWords = DefaultMaxWords,
                OverlapWords = DefaultOverlapWords
            });
        }

        private static string? Validate(ChunkSettingsDto settings)
        {
            if (settings.MaxWords < MinMaxWords || settings.MaxWords > MaxMaxWords)
                return $"Số từ tối đa mỗi chunk phải từ {MinMaxWords} đến {MaxMaxWords}.";
            if (settings.OverlapWords < 0 || settings.OverlapWords > MaxOverlapWords)
                return $"Số từ chồng lấn phải từ 0 đến {MaxOverlapWords}.";
            if (settings.OverlapWords >= settings.MaxWords)
                return "Số từ chồng lấn phải nhỏ hơn số từ tối đa mỗi chunk.";
            return null;
        }

        private ChunkSettingsDto LoadFromFile()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var settings = JsonSerializer.Deserialize<ChunkSettingsDto>(File.ReadAllText(_filePath));
                    if (settings != null && Validate(settings) == null)
                    {
                        return settings;
                    }
                }
            }
            catch
            {
                // File hỏng hoặc không đọc được → dùng giá trị mặc định
            }
            return new ChunkSettingsDto
            {
                MaxWords = DefaultMaxWords,
                OverlapWords = DefaultOverlapWords
            };
        }
    }
}
