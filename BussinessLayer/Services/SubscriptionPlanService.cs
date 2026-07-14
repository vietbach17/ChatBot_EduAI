using BussinessLayer.IServices;
using BussinessLayer.IGateways;
using BussinessLayer.Gateways;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using DataAccessLayer.IRepositories;

namespace BussinessLayer.Services
{
    public class SubscriptionPlanService : ISubscriptionPlanService
    {
        private readonly ISubscriptionPlanRepository _repo;

        public SubscriptionPlanService(ISubscriptionPlanRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<SubscriptionPlanDto>> GetAllAsync()
        {
            var plans = await _repo.GetAllAsync();
            return plans.Select(ToDto).ToList();
        }

        public async Task<SubscriptionPlanDto?> GetByIdAsync(int id)
        {
            var p = await _repo.GetByIdAsync(id);
            return p == null ? null : ToDto(p);
        }

        public async Task<(bool Success, string Error)> CreateAsync(SubscriptionPlanDto dto)
        {
            var name = dto.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name)) return (false, "Tên gói không được để trống.");

            // Kiểm tra tên trùng
            var existing = await _repo.GetByNameAsync(name);
            if (existing != null)
                return (false, $"Tên gói '{name}' đã tồn tại.");

            await _repo.AddAsync(new SubscriptionPlan
            {
                Name                 = name,
                Description          = dto.Description?.Trim() ?? string.Empty,
                Price                = dto.Price,
                MonthlyQuestionLimit = dto.MonthlyQuestionLimit,
                IsActive             = dto.IsActive,
                SortOrder            = dto.SortOrder,
                Features             = dto.Features ?? string.Empty,
                DurationDays         = dto.DurationDays
            });
            return (true, string.Empty);
        }

        public async Task<(bool Success, string Error)> UpdateAsync(SubscriptionPlanDto dto)
        {
            var plan = await _repo.GetByIdAsync(dto.Id);
            if (plan == null) return (false, "Không tìm thấy gói.");

            var name = dto.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name)) return (false, "Tên gói không được để trống.");

            // Kiểm tra tên trùng với gói khác
            var dup = await _repo.GetByNameAsync(name);
            if (dup != null && dup.Id != dto.Id)
                return (false, $"Tên gói '{name}' đã được sử dụng.");

            plan.Name                 = name;
            plan.Description          = dto.Description?.Trim() ?? string.Empty;
            plan.Price                = dto.Price;
            plan.MonthlyQuestionLimit = dto.MonthlyQuestionLimit;
            plan.IsActive             = dto.IsActive;
            plan.SortOrder            = dto.SortOrder;
            plan.Features             = dto.Features ?? string.Empty;
            plan.DurationDays         = dto.DurationDays;

            await _repo.UpdateAsync(plan);
            return (true, string.Empty);
        }

        public async Task<(bool Success, string Error)> DeleteAsync(int id)
        {
            var plan = await _repo.GetByIdAsync(id);
            if (plan == null) return (false, "Không tìm thấy gói.");

            await _repo.DeleteAsync(id);
            return (true, string.Empty);
        }

        private static SubscriptionPlanDto ToDto(SubscriptionPlan p) => new()
        {
            Id                   = p.Id,
            Name                 = p.Name,
            Description          = p.Description,
            Price                = p.Price,
            MonthlyQuestionLimit = p.MonthlyQuestionLimit,
            IsActive             = p.IsActive,
            SortOrder            = p.SortOrder,
            Features             = p.Features,
            DurationDays         = p.DurationDays
        };
    }
}

