using DataAccessLayer.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pgvector;
namespace DataAccessLayer.IRepositories
{
    /// <summary>
    /// Giao diện Repository truy vấn Tài liệu, DocumentChunk và Embedding.
    /// </summary>
    public interface IDocumentRepository
    {
        Task<List<Document>> GetAllDocumentsAsync();
        Task<Document?> GetDocumentByIdAsync(int id);
        Task<List<Document>> GetDocumentsByIdsAsync(IEnumerable<int> ids);
        Task<List<Document>> GetDocumentsBySubjectIdAsync(int subjectId);
        Task AddDocumentAsync(Document document);
        Task UpdateDocumentAsync(Document document);
        Task AddDocumentChunksAsync(IEnumerable<DocumentChunk> chunks);
        Task<List<DocumentChunk>> GetDocumentChunksAsync(int documentId);
        Task<List<DocumentChunk>> SearchSimilarChunksAsync(Vector embedding, int? subjectId = null, int topK = 5);
        Task<bool> DeleteDocumentAsync(int id);
        Task<Document?> GetDocumentByIdWithUploaderAsync(int id);
    }
}
