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

                        // Lấy các User đã quá thời hạn 5 giờ và QuotaResetDate != null
                        var expiredUsers = await context.Users
                            .Where(u => u.QuotaResetDate != null && now >= u.QuotaResetDate)
                            .ToListAsync(stoppingToken);

                        if (expiredUsers.Any())
                        {
                            foreach (var user in expiredUsers)
                            {
                                user.MonthlyQuestionCount = 0;
                                user.QuotaResetDate = null; // Đặt về null, chờ câu hỏi tiếp theo kích hoạt lại
                            }
                            await context.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation($"[BackgroundService] Đã reset quota cho {expiredUsers.Count} người dùng.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong quá trình quét reset Quota ngầm.");
                }

                // Chờ 1 phút rồi quét lại
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
