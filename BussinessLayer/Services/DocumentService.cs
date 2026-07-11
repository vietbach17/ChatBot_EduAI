using BussinessLayer.IServices;
using BussinessLayer.IGateways;
using BussinessLayer.Gateways;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using DataAccessLayer.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessLayer.Services
{
    public class DocumentService : IDocumentService
    {
         private readonly IDocumentRepository _documentRepository;
        private readonly IGeminiService _geminiService;

        public DocumentService(IDocumentRepository documentRepository, IGeminiService geminiService)
        {
            _documentRepository = documentRepository;
            _geminiService = geminiService;
        }

        public async Task<IEnumerable<DocumentDto>> GetAllDocumentsAsync()
        {
            var documents = await _documentRepository.GetAllDocumentsAsync();
            return documents.Select(d => new DocumentDto
            {
                Id = d.Id,
                Title = d.Title,
                FileType = d.FileType,
                Status = Enum.Parse<DocumentStatus>(d.Status),
                UploadedAt = d.UploadedAt,
                FileUrl = d.FileUrl,
                SubjectId = d.SubjectId,
                ChapterId = d.ChapterId,
                SubjectName = d.Subject?.Name,
                ChapterTitle = d.Chapter?.Title,
                UploaderId = d.UploaderId,
                UploaderName = d.Uploader?.Username
            }).ToList();
        }

        public async Task<IEnumerable<DocumentDto>> GetDocumentsBySubjectAsync(int subjectId)
        {
            var documents = await _documentRepository.GetAllDocumentsAsync(); // Should add a specialized repo method
            return documents.Where(d => d.SubjectId == subjectId).Select(d => new DocumentDto
            {
                Id = d.Id,
                Title = d.Title,
                FileType = d.FileType,
                Status = Enum.Parse<DocumentStatus>(d.Status),
                UploadedAt = d.UploadedAt,
                FileUrl = d.FileUrl,
                SubjectId = d.SubjectId,
                ChapterId = d.ChapterId,
                UploaderId = d.UploaderId,
                UploaderName = d.Uploader?.Username
            }).ToList();
        }

        public async Task<IEnumerable<DocumentDto>> GetDocumentsByChapterAsync(int chapterId)
        {
            var documents = await _documentRepository.GetAllDocumentsAsync(); 
            return documents.Where(d => d.ChapterId == chapterId).Select(d => new DocumentDto
            {
                Id = d.Id,
                Title = d.Title,
                FileType = d.FileType,
                Status = Enum.Parse<DocumentStatus>(d.Status),
                UploadedAt = d.UploadedAt,
                FileUrl = d.FileUrl,
                SubjectId = d.SubjectId,
                ChapterId = d.ChapterId,
                UploaderId = d.UploaderId,
                UploaderName = d.Uploader?.Username
            }).ToList();
        }

        public async Task<IEnumerable<DocumentDto>> GetDocumentsByUploaderAsync(int uploaderId)
        {
            var documents = await _documentRepository.GetAllDocumentsAsync(); // In real app, filter in DB
            return documents.Where(d => d.UploaderId == uploaderId).Select(d => new DocumentDto
            {
                Id = d.Id,
                Title = d.Title,
                FileType = d.FileType,
                Status = Enum.Parse<DocumentStatus>(d.Status),
                UploadedAt = d.UploadedAt,
                FileUrl = d.FileUrl,
                SubjectId = d.SubjectId,
                ChapterId = d.ChapterId,
                SubjectName = d.Subject?.Name,
                ChapterTitle = d.Chapter?.Title,
                UploaderId = d.UploaderId,
                UploaderName = d.Uploader?.Username
            }).ToList();
        }

        public async Task<DocumentDto?> GetDocumentByIdAsync(int id)
        {
            // Use the version that includes Uploader navigation
            var doc = await _documentRepository.GetDocumentByIdWithUploaderAsync(id);
            if (doc == null) return null;

            return new DocumentDto
            {
                Id = doc.Id,
                Title = doc.Title,
                FileType = doc.FileType,
                FileUrl = doc.FileUrl,
                Content = doc.Content,
                Status = Enum.Parse<DocumentStatus>(doc.Status),
                UploadedAt = doc.UploadedAt,
                SubjectId = doc.SubjectId,
                ChapterId = doc.ChapterId,
                UploaderId = doc.UploaderId,
                UploaderName = doc.Uploader?.Username,
                SubjectName = doc.Subject?.Name,
                ChapterTitle = doc.Chapter?.Title,
                IsDeleted = doc.IsDeleted
            };
        }

        public async Task<string> GetDocumentTextAsync(int id)
        {
            var doc = await _documentRepository.GetDocumentByIdAsync(id);
            return doc?.Content ?? string.Empty;
        }


        public async Task<bool> UpdateDocumentChapterAsync(int documentId, int? chapterId)
        {
            var document = await _documentRepository.GetDocumentByIdAsync(documentId);
            if (document == null) return false;

            document.ChapterId = chapterId;
            await _documentRepository.UpdateDocumentAsync(document);
            return true;
        }

        public async Task<int> AddDocumentAsync(string title, string fileType, string fileUrl, int? subjectId, int? chapterId, int? uploaderId)
        {
            return await AddDocumentAsync(title, fileType, fileUrl, subjectId, chapterId, uploaderId, null);
        }

        public async Task<int> AddDocumentAsync(string title, string fileType, string fileUrl, int? subjectId, int? chapterId, int? uploaderId, string? extractedContent)
        {
            var document = new DataAccessLayer.Entities.Document
            {
                Title = title,
                FileType = fileType,
                FileUrl = fileUrl,
                SubjectId = subjectId,
                ChapterId = chapterId,
                UploaderId = uploaderId,
                Status = "Indexed",
                UploadedAt = System.DateTime.UtcNow,
                Content = extractedContent ?? string.Empty
            };
            await _documentRepository.AddDocumentAsync(document);
            return document.Id;
        }

        public async Task<bool> ProcessDocumentAsync(int documentId, string extractedContent)
        {
            var doc = await _documentRepository.GetDocumentByIdAsync(documentId);
            if (doc == null) return false;
            doc.Content = extractedContent;
            doc.Status = "Indexed";
            // await _documentRepository.UpdateDocumentAsync(doc); // Needs Update method
            return true;
        }

        public async Task<bool> ProcessDocumentEmbeddingAsync(int documentId, System.Func<int, int, Task>? progressCallback = null)
        {
            var doc = await _documentRepository.GetDocumentByIdAsync(documentId);
            if (doc == null || string.IsNullOrWhiteSpace(doc.Content)) return false;

            // Context-Aware Chunking Strategy: split by semantics and maintain overlap
            var textChunks = SplitTextByContext(doc.Content, maxWords: 300, overlapWords: 50);
            
            var chunks = new List<DataAccessLayer.Entities.DocumentChunk>();
            int orderIndex = 1;
            int totalChunks = textChunks.Count;

            foreach (var chunkText in textChunks)
            {
                // Call Gemini for embedding
                var vector = await _geminiService.GetEmbeddingAsync(chunkText);

                chunks.Add(new DataAccessLayer.Entities.DocumentChunk
                {
                    DocumentId = documentId,
                    Content = chunkText,
                    Embedding = new Pgvector.Vector(vector),
                    OrderIndex = orderIndex++
                });
                
                if (progressCallback != null)
                {
                    await progressCallback(chunks.Count, totalChunks);
                }
            }

            if (chunks.Any())
            {
                await _documentRepository.AddDocumentChunksAsync(chunks);
            }
            
            if (progressCallback != null)
            {
                await progressCallback(1, 1); // Đảm bảo lên 100% khi hoàn thành
            }
            
            return true;
        }

        private List<string> SplitTextByContext(string content, int maxWords = 300, int overlapWords = 50)
        {
            var chunks = new List<string>();
            if (string.IsNullOrWhiteSpace(content)) return chunks;

            // Bước 1: Tách đoạn văn
            var paragraphs = System.Text.RegularExpressions.Regex.Split(content, @"\n\s*\n");
            
            var currentChunkWords = new List<string>();
            
            foreach (var para in paragraphs)
            {
                if (string.IsNullOrWhiteSpace(para)) continue;

                var paraWords = para.Split(new[] { ' ', '\r', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                
                // Nếu đoạn văn dài hơn giới hạn, ta chia nhỏ nó thành các câu
                if (paraWords.Length > maxWords)
                {
                    // Tách câu giữ nguyên dấu kết thúc câu
                    var sentences = System.Text.RegularExpressions.Regex.Split(para, @"(?<=[.!?])\s+");
                    
                    foreach (var sentence in sentences)
                    {
                        if (string.IsNullOrWhiteSpace(sentence)) continue;
                        
                        var sentenceWords = sentence.Split(new[] { ' ', '\r', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                        
                        // Nếu 1 câu dài hơn maxWords (hiếm nhưng có thể xảy ra ở PDF lỗi), tách ép theo số từ
                        if (sentenceWords.Length > maxWords)
                        {
                            foreach (var word in sentenceWords)
                            {
                                currentChunkWords.Add(word);
                                if (currentChunkWords.Count >= maxWords)
                                {
                                    chunks.Add(string.Join(" ", currentChunkWords));
                                    var overlap = currentChunkWords.Skip(currentChunkWords.Count - overlapWords).ToList();
                                    currentChunkWords.Clear();
                                    currentChunkWords.AddRange(overlap);
                                }
                            }
                        }
                        else
                        {
                            if (currentChunkWords.Count + sentenceWords.Length > maxWords && currentChunkWords.Count > 0)
                            {
                                chunks.Add(string.Join(" ", currentChunkWords));
                                var overlap = currentChunkWords.Skip(currentChunkWords.Count - overlapWords).ToList();
                                currentChunkWords.Clear();
                                currentChunkWords.AddRange(overlap);
                            }
                            currentChunkWords.AddRange(sentenceWords);
                        }
                    }
                }
                else
                {
                    if (currentChunkWords.Count + paraWords.Length > maxWords && currentChunkWords.Count > 0)
                    {
                        chunks.Add(string.Join(" ", currentChunkWords));
                        var overlap = currentChunkWords.Skip(currentChunkWords.Count - overlapWords).ToList();
                        currentChunkWords.Clear();
                        currentChunkWords.AddRange(overlap);
                    }
                    currentChunkWords.AddRange(paraWords);
                }
            }

            if (currentChunkWords.Count > 0)
            {
                chunks.Add(string.Join(" ", currentChunkWords));
            }

            return chunks;
        }

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            return await _documentRepository.DeleteDocumentAsync(id);
        }

        public async Task<IEnumerable<string>> GetDocumentChunksAsync(int id)
        {
            var chunks = await _documentRepository.GetDocumentChunksAsync(id);
            return chunks?.OrderBy(c => c.OrderIndex).Select(c => c.Content) ?? new List<string>();
        }
    }
}

