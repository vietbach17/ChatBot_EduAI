using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    /// <summary>
    /// Repository truy vấn Tài liệu, DocumentChunk, và thực hiện tìm kiếm vector (pgvector).
    /// </summary>
    public class DocumentRepository : IDocumentRepository
    {
        private readonly ApplicationDbContext _context;

        public DocumentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Document>> GetAllDocumentsAsync()
        {
            var data = await _context.Documents
                .Select(d => new
                {
                    d.Id,
                    d.Title,
                    d.FileType,
                    d.FileUrl,
                    d.Status,
                    d.UploadedAt,
                    d.SubjectId,
                    d.ChapterId,
                    d.UploaderId,
                    SubjectName = d.Subject != null ? d.Subject.Name : null,
                    ChapterTitle = d.Chapter != null ? d.Chapter.Title : null,
                    UploaderUsername = d.Uploader != null ? d.Uploader.Username : null
                })
                .ToListAsync();

            return data.Select(x => new Document
            {
                Id = x.Id,
                Title = x.Title,
                FileType = x.FileType,
                FileUrl = x.FileUrl,
                Status = x.Status,
                UploadedAt = x.UploadedAt,
                SubjectId = x.SubjectId,
                ChapterId = x.ChapterId,
                UploaderId = x.UploaderId,
                Subject = x.SubjectName != null ? new Subject { Name = x.SubjectName } : null,
                Chapter = x.ChapterTitle != null ? new Chapter { Title = x.ChapterTitle } : null,
                Uploader = x.UploaderUsername != null ? new User { Username = x.UploaderUsername } : null
            }).ToList();
        }

        public async Task<Document?> GetDocumentByIdAsync(int id)
        {
            return await _context.Documents.FindAsync(id);
        }

        public async Task<List<Document>> GetDocumentsByIdsAsync(IEnumerable<int> ids)
        {
            return await _context.Documents
                .Where(d => ids.Contains(d.Id))
                .ToListAsync();
        }

        public async Task<List<Document>> GetDocumentsBySubjectIdAsync(int subjectId)
        {
            return await _context.Documents
                .Include(d => d.Subject)
                .Include(d => d.Chapter)
                .Where(d => d.SubjectId == subjectId && d.Status == "Indexed")
                .ToListAsync();
        }

        public async Task AddDocumentAsync(Document document)
        {
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateDocumentAsync(Document document)
        {
            _context.Documents.Update(document);
            await _context.SaveChangesAsync();
        }

        public async Task AddDocumentChunksAsync(IEnumerable<DocumentChunk> chunks)
        {
            _context.DocumentChunks.AddRange(chunks);
            await _context.SaveChangesAsync();
        }

        public async Task<List<DocumentChunk>> GetDocumentChunksAsync(int documentId)
        {
            return await _context.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .OrderBy(c => c.OrderIndex)
                .ToListAsync();
        }

        public async Task<List<DocumentChunk>> SearchSimilarChunksAsync(Vector embedding, int? subjectId = null, int topK = 5)
        {
            var chunkQuery = _context.DocumentChunks
                .Include(c => c.Document)
                    .ThenInclude(d => d.Subject)
                .Include(c => c.Document)
                    .ThenInclude(d => d.Chapter)
                .Where(c => c.Embedding != null);

            if (subjectId.HasValue && subjectId.Value > 0)
            {
                chunkQuery = chunkQuery.Where(c => c.Document.SubjectId == subjectId.Value);
            }

            return await chunkQuery
                .OrderBy(c => c.Embedding!.CosineDistance(embedding))
                .ThenBy(c => c.DocumentId)
                .ThenBy(c => c.OrderIndex)
                .Take(topK)
                .ToListAsync();
        }

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return false;
            var chunks = _context.DocumentChunks.Where(c => c.DocumentId == id);
            _context.DocumentChunks.RemoveRange(chunks);

            doc.IsDeleted = true;
            _context.Documents.Update(doc);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Document?> GetDocumentByIdWithUploaderAsync(int id)
        {
            return await _context.Documents
                .IgnoreQueryFilters()
                .Include(d => d.Uploader)
                .Include(d => d.Subject)
                .Include(d => d.Chapter)
                .FirstOrDefaultAsync(d => d.Id == id);
        }
    }
}
