using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Dịch vụ Chính sách Chunk File. Lưu template mặc định (số từ tối đa mỗi chunk, số từ chồng lấn)
    /// cùng quyền và khoảng giá trị Giảng viên được phép tự cấu hình vào file JSON
    /// để giữ giá trị qua các lần khởi động lại app. Đăng ký dạng Singleton.
    /// </summary>
    public class ChunkSettingsService : IChunkSettingsService
    {
        public const int DefaultMaxWords = 300;
        public const int DefaultOverlapWords = 50;
        public const int MinMaxWords = 50;
        public const int MaxMaxWords = 2000;
        public const int MaxOverlapWords = 500;
        public const int DefaultLecturerMinWords = 100;
        public const int DefaultLecturerMaxWords = 800;
        public const int DefaultLecturerMaxOverlapWords = 300;

        private readonly string _filePath;
        private readonly object _lock = new object();
        private ChunkPolicyDto _current;

        public ChunkSettingsService(string filePath)
        {
            _filePath = filePath;
            _current = LoadFromFile();
        }

        public ChunkSettingsDto GetSettings() => GetPolicy().ToSettings();

        public ChunkPolicyDto GetPolicy()
        {
            lock (_lock)
            {
                return Clone(_current);
            }
        }

        public Task<(bool Success, string? Error)> UpdateAsync(ChunkPolicyDto policy)
        {
            var error = Validate(policy);
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
                    var json = JsonSerializer.Serialize(policy, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_filePath, json);
                    _current = Clone(policy);
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
            return UpdateAsync(CreateDefault());
        }

        public string? ValidateLecturerSettings(ChunkSettingsDto settings)
        {
            var policy = GetPolicy();

            if (!policy.AllowLecturerOverride)
                return "Quản trị viên hiện không cho phép Giảng viên tự cấu hình chunk.";
            if (settings.MaxWords < policy.LecturerMinWords || settings.MaxWords > policy.LecturerMaxWords)
                return $"Số từ tối đa mỗi chunk phải từ {policy.LecturerMinWords} đến {policy.LecturerMaxWords}.";
            if (settings.OverlapWords < 0 || settings.OverlapWords > policy.LecturerMaxOverlapWords)
                return $"Số từ chồng lấn phải từ 0 đến {policy.LecturerMaxOverlapWords}.";
            if (settings.OverlapWords >= settings.MaxWords)
                return "Số từ chồng lấn phải nhỏ hơn số từ tối đa mỗi chunk.";
            return null;
        }

        private static string? Validate(ChunkPolicyDto policy)
        {
            if (policy.MaxWords < MinMaxWords || policy.MaxWords > MaxMaxWords)
                return $"Số từ tối đa mỗi chunk phải từ {MinMaxWords} đến {MaxMaxWords}.";
            if (policy.OverlapWords < 0 || policy.OverlapWords > MaxOverlapWords)
                return $"Số từ chồng lấn phải từ 0 đến {MaxOverlapWords}.";
            if (policy.OverlapWords >= policy.MaxWords)
                return "Số từ chồng lấn phải nhỏ hơn số từ tối đa mỗi chunk.";

            if (policy.LecturerMinWords < MinMaxWords || policy.LecturerMinWords > MaxMaxWords)
                return $"Giới hạn dưới cho Giảng viên phải từ {MinMaxWords} đến {MaxMaxWords}.";
            if (policy.LecturerMaxWords < MinMaxWords || policy.LecturerMaxWords > MaxMaxWords)
                return $"Giới hạn trên cho Giảng viên phải từ {MinMaxWords} đến {MaxMaxWords}.";
            if (policy.LecturerMinWords > policy.LecturerMaxWords)
                return "Giới hạn dưới cho Giảng viên phải nhỏ hơn hoặc bằng giới hạn trên.";
            if (policy.LecturerMaxOverlapWords < 0 || policy.LecturerMaxOverlapWords > MaxOverlapWords)
                return $"Giới hạn chồng lấn cho Giảng viên phải từ 0 đến {MaxOverlapWords}.";
            if (policy.LecturerMaxOverlapWords >= policy.LecturerMaxWords)
                return "Giới hạn chồng lấn cho Giảng viên phải nhỏ hơn giới hạn trên số từ mỗi chunk.";
            return null;
        }

        private ChunkPolicyDto LoadFromFile()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    // File cũ chỉ chứa MaxWords/OverlapWords — các trường chính sách mới sẽ nhận giá trị mặc định.
                    var policy = JsonSerializer.Deserialize<ChunkPolicyDto>(File.ReadAllText(_filePath));
                    if (policy != null && Validate(policy) == null)
                    {
                        return policy;
                    }
                }
            }
            catch
            {
                // File hỏng hoặc không đọc được → dùng giá trị mặc định
            }
            return CreateDefault();
        }

        private static ChunkPolicyDto CreateDefault() => new ChunkPolicyDto
        {
            MaxWords = DefaultMaxWords,
            OverlapWords = DefaultOverlapWords,
            AllowLecturerOverride = true,
            LecturerMinWords = DefaultLecturerMinWords,
            LecturerMaxWords = DefaultLecturerMaxWords,
            LecturerMaxOverlapWords = DefaultLecturerMaxOverlapWords
        };

        private static ChunkPolicyDto Clone(ChunkPolicyDto source) => new ChunkPolicyDto
        {
            MaxWords = source.MaxWords,
            OverlapWords = source.OverlapWords,
            AllowLecturerOverride = source.AllowLecturerOverride,
            LecturerMinWords = source.LecturerMinWords,
            LecturerMaxWords = source.LecturerMaxWords,
            LecturerMaxOverlapWords = source.LecturerMaxOverlapWords
        };
    }
}
