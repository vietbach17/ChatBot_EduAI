using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace BussinessLayer.Services
{
    public class QuestionBankService : IQuestionBankService
    {
        private readonly IQuestionBankRepository _repository;
        private readonly ApplicationDbContext _context;

        public QuestionBankService(IQuestionBankRepository repository, ApplicationDbContext context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task<QuestionBankDto?> GetQuestionByIdAsync(int id)
        {
            var q = await _repository.GetByIdAsync(id);
            if (q == null) return null;

            return MapToDto(q);
        }

        public async Task<(IEnumerable<QuestionBankDto> Items, int TotalCount)> GetPagedQuestionsAsync(
            int subjectId,
            string? difficulty,
            string? type,
            string? search,
            int page,
            int pageSize)
        {
            var questions = await _repository.GetPagedAsync(subjectId, difficulty, type, search, page, pageSize);
            var totalCount = await _repository.CountAsync(subjectId, difficulty, type, search);

            var dtos = questions.Select(MapToDto);
            return (dtos, totalCount);
        }

        public async Task<bool> AddQuestionAsync(CreateQuestionDto createDto, int lecturerId)
        {
            if (createDto == null) return false;

            var question = new QuestionBank
            {
                SubjectId = createDto.SubjectId,
                Content = createDto.Content,
                QuestionType = createDto.QuestionType,
                OptionsJson = createDto.OptionsJson,
                CorrectAnswer = createDto.CorrectAnswer,
                Difficulty = createDto.Difficulty,
                Tags = createDto.Tags,
                IsAIGenerated = createDto.IsAIGenerated,
                LecturerId = lecturerId,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(question);
            return true;
        }

        public async Task<bool> UpdateQuestionAsync(int id, CreateQuestionDto updateDto)
        {
            var question = await _repository.GetByIdAsync(id);
            if (question == null || updateDto == null) return false;

            question.SubjectId = updateDto.SubjectId;
            question.Content = updateDto.Content;
            question.QuestionType = updateDto.QuestionType;
            question.OptionsJson = updateDto.OptionsJson;
            question.CorrectAnswer = updateDto.CorrectAnswer;
            question.Difficulty = updateDto.Difficulty;
            question.Tags = updateDto.Tags;

            await _repository.UpdateAsync(question);
            return true;
        }

        public async Task<bool> DeleteQuestionAsync(int id)
        {
            var question = await _repository.GetByIdAsync(id);
            if (question == null) return false;

            await _repository.DeleteAsync(id);
            return true;
        }

        public async Task<IEnumerable<Subject>> GetAllSubjectsAsync()
        {
            return await _context.Subjects.OrderBy(s => s.Code).ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetQuestionStatisticsAsync(int subjectId)
        {
            var query = _context.QuestionBanks.Where(q => !q.IsDeleted).AsQueryable();
            if (subjectId > 0)
            {
                query = query.Where(q => q.SubjectId == subjectId);
            }

            var stats = await query
                .GroupBy(q => q.Difficulty)
                .Select(g => new { Difficulty = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Difficulty, g => g.Count);

            var difficulties = new[] { "Easy", "Medium", "Hard" };
            foreach (var diff in difficulties)
            {
                if (!stats.ContainsKey(diff))
                {
                    stats[diff] = 0;
                }
            }

            return stats;
        }

        private QuestionBankDto MapToDto(QuestionBank q)
        {
            return new QuestionBankDto
            {
                Id = q.Id,
                SubjectId = q.SubjectId,
                SubjectCode = q.Subject?.Code ?? string.Empty,
                SubjectName = q.Subject?.Name ?? string.Empty,
                Content = q.Content,
                QuestionType = q.QuestionType,
                OptionsJson = q.OptionsJson,
                CorrectAnswer = q.CorrectAnswer,
                Difficulty = q.Difficulty,
                Tags = q.Tags,
                IsAIGenerated = q.IsAIGenerated,
                LecturerId = q.LecturerId,
                LecturerUsername = q.Lecturer?.Username ?? string.Empty,
                CreatedAt = q.CreatedAt
            };
        }

        public async Task<(IEnumerable<QuestionBankDto> Items, int TotalCount)> GetDeletedPagedQuestionsAsync(int page, int pageSize)
        {
            var questions = await _repository.GetDeletedPagedAsync(page, pageSize);
            var totalCount = await _repository.CountDeletedAsync();
            var dtos = questions.Select(MapToDto);
            return (dtos, totalCount);
        }

        public async Task<bool> RestoreQuestionAsync(int id)
        {
            await _repository.RestoreAsync(id);
            return true;
        }

        public async Task<bool> HardDeleteQuestionAsync(int id)
        {
            await _repository.HardDeleteAsync(id);
            return true;
        }
    }
}
