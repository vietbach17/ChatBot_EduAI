using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Dịch vụ Báo cáo / Khiếu nại. Xử lý việc gửi báo cáo từ Student/Lecturer,
    /// truy xuất danh sách cho người gửi và Admin, và cho phép Admin phản hồi / cập nhật trạng thái.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly IReportRepository _repository;

        private static readonly string[] ValidStatuses = { "Pending", "InProgress", "Resolved", "Rejected" };

        public ReportService(IReportRepository repository)
        {
            _repository = repository;
        }

        public async Task<int> CreateReportAsync(CreateReportDto dto)
        {
            var report = new Report
            {
                ReporterId = dto.ReporterId,
                ReporterRole = dto.ReporterRole,
                Category = dto.Category,
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                ImageUrl = dto.ImageUrl,
                RelatedTransactionId = dto.RelatedTransactionId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            await _repository.AddAsync(report);
            return report.Id;
        }

        public async Task<IEnumerable<ReportDto>> GetMyReportsAsync(int reporterId)
        {
            var reports = await _repository.GetByReporterIdAsync(reporterId);
            return reports.Select(MapToDto).ToList();
        }

        public async Task<IEnumerable<ReportDto>> GetAllReportsAsync(string? role = null, string? status = null, string? category = null)
        {
            var reports = await _repository.GetAllAsync();
            var query = reports.AsEnumerable();

            if (!string.IsNullOrEmpty(role))
                query = query.Where(r => r.ReporterRole == role);
            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);
            if (!string.IsNullOrEmpty(category))
                query = query.Where(r => r.Category == category);

            return query.Select(MapToDto).ToList();
        }

        public async Task<ReportDto?> GetReportByIdAsync(int id)
        {
            var report = await _repository.GetByIdAsync(id);
            return report == null ? null : MapToDto(report);
        }

        public async Task<bool> RespondAsync(ReportRespondDto dto)
        {
            if (!ValidStatuses.Contains(dto.Status)) return false;

            var report = await _repository.GetByIdAsync(dto.ReportId);
            if (report == null) return false;

            report.Status = dto.Status;
            report.AdminResponse = dto.AdminResponse?.Trim();
            report.HandledByAdminId = dto.AdminId;
            report.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(report);
            return true;
        }

        public async Task<Dictionary<string, int>> GetStatusCountsAsync()
        {
            var reports = await _repository.GetAllAsync();
            var counts = reports
                .GroupBy(r => r.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var s in ValidStatuses)
                if (!counts.ContainsKey(s)) counts[s] = 0;

            return counts;
        }

        private static ReportDto MapToDto(Report r) => new ReportDto
        {
            Id = r.Id,
            ReporterId = r.ReporterId,
            ReporterName = r.Reporter?.Username ?? "Unknown",
            ReporterRole = r.ReporterRole,
            Category = r.Category,
            CategoryLabel = ReportCategories.GetCategoryLabel(r.ReporterRole, r.Category),
            Title = r.Title,
            Description = r.Description,
            ImageUrl = r.ImageUrl,
            RelatedTransactionId = r.RelatedTransactionId,
            Status = r.Status,
            StatusLabel = ReportCategories.GetStatusLabel(r.Status),
            AdminResponse = r.AdminResponse,
            HandledByAdminName = r.HandledByAdmin?.Username,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
    }
}
