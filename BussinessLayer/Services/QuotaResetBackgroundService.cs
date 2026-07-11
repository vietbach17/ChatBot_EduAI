using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BussinessLayer.Services
{
    public class QuotaResetBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<QuotaResetBackgroundService> _logger;

        public QuotaResetBackgroundService(IServiceProvider serviceProvider, ILogger<QuotaResetBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var now = DateTime.UtcNow;

                        // Lấy các User đã quá thời hạn 1 tháng (QuotaResetDate)
                        var expiredMonthlyUsers = await context.Users
                            .Where(u => u.QuotaResetDate != null && now >= u.QuotaResetDate)
                            .ToListAsync(stoppingToken);

                        if (expiredMonthlyUsers.Any())
                        {
                            foreach (var user in expiredMonthlyUsers)
                            {
                                user.MonthlyQuestionCount = 0;
                                user.QuotaResetDate = null; // Đặt về null, chờ câu hỏi tiếp theo kích hoạt lại
                            }
                            await context.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation($"[BackgroundService] Đã reset quota THÁNG cho {expiredMonthlyUsers.Count} người dùng.");
                        }

                        // Lấy các User đã quá thời hạn 5 giờ (ShortTermResetDate)
                        var expiredShortTermUsers = await context.Users
                            .Where(u => u.ShortTermResetDate != null && now >= u.ShortTermResetDate)
                            .ToListAsync(stoppingToken);

                        if (expiredShortTermUsers.Any())
                        {
                            foreach (var user in expiredShortTermUsers)
                            {
                                user.ShortTermQuestionCount = 0;
                                user.ShortTermResetDate = null; // Đặt về null, chờ câu hỏi tiếp theo kích hoạt lại
                            }
                            await context.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation($"[BackgroundService] Đã reset quota 5 GIỜ cho {expiredShortTermUsers.Count} người dùng.");
                        }
                    }

                    // Chờ 1 phút rồi quét lại
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Bỏ qua lỗi khi BackgroundService đang bị hủy do ứng dụng tắt
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong quá trình quét reset Quota ngầm.");
                }
            }
        }
    }
}
