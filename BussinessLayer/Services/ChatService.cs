using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using DataAccessLayer.IRepositories;
using Pgvector;

using BussinessLayer.IServices;

namespace BussinessLayer.Services
{
    public class ChatService : IChatService
    {
        private const string DefaultSessionTitle = "Cuộc trò chuyện mới";

        private readonly IChatRepository _chatRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IGeminiService _geminiService;
        private readonly IUserRepository _userRepository;

        public static int GetMonthlyLimit(string plan) => plan switch
        {
            "Basic" => 100,
            "Premium" => int.MaxValue,
            _ => 5
        };

        public ChatService(
            IChatRepository chatRepository,
            IDocumentRepository documentRepository,
            IGeminiService geminiService,
            IUserRepository userRepository)
        {
            _chatRepository = chatRepository;
            _documentRepository = documentRepository;
            _geminiService = geminiService;
            _userRepository = userRepository;
        }

        public async Task<List<ChatSessionDto>> GetUserSessionsAsync(int userId)
        {
            var sessions = await _chatRepository.GetUserSessionsAsync(userId);
            return sessions.Select(MapSession).ToList();
        }

        public async Task<ChatSessionDto?> CreateSessionAsync(int userId)
        {
            var session = await _chatRepository.CreateSessionAsync(userId, DefaultSessionTitle);
            return MapSession(session);
        }

        public async Task<bool> DeleteSessionAsync(int userId, int sessionId)
        {
            var session = await _chatRepository.GetSessionByIdAsync(sessionId);
            if (session == null || session.UserId != userId)
            {
                return false;
            }

            await _chatRepository.DeleteSessionAsync(sessionId);
            return true;
        }

        public async Task<bool> ClearSessionAsync(int userId, int sessionId)
        {
            var session = await _chatRepository.GetSessionByIdAsync(sessionId);
            if (session == null || session.UserId != userId)
            {
                return false;
            }

            await _chatRepository.ClearSessionAsync(sessionId);
            return true;
        }

        public async Task<ChatResponseDto> ProcessChatMessageAsync(int userId, ChatRequestDto request)
        {
            try
            {
                var existingSession = await ResolveSessionAsync(userId, request.SessionId, request.Message, createIfMissing: false);
                var conversationHistory = BuildConversationHistory(existingSession?.Messages);

                var user = await _userRepository.GetUserByIdAsync(userId);
                var effectivePlan = "Free";
                var limit = GetMonthlyLimit("Free");
                var remainingBefore = int.MaxValue;
                var remainingAfter = int.MaxValue;

                if (user != null)
                {
                    var now = DateTime.UtcNow;
                    if (user.QuotaResetDate == null || now >= user.QuotaResetDate)
                    {
                        user.MonthlyQuestionCount = 0;
                        user.QuotaResetDate = DateTime.SpecifyKind(
                            new DateTime(now.Year, now.Month, 1).AddMonths(1),
                            DateTimeKind.Utc);
                        await _userRepository.UpdateUserAsync(user);
                    }

                    var planActive = user.SubscriptionPlan == "Free" ||
                                     (user.SubscriptionExpiry.HasValue && user.SubscriptionExpiry.Value >= now);
                    effectivePlan = planActive ? user.SubscriptionPlan : "Free";

                    limit = GetMonthlyLimit(effectivePlan);
                    remainingBefore = limit == int.MaxValue ? int.MaxValue : Math.Max(0, limit - user.MonthlyQuestionCount);
                    remainingAfter = remainingBefore == int.MaxValue ? int.MaxValue : Math.Max(0, remainingBefore - 1);

                    if (user.MonthlyQuestionCount >= limit)
                    {
                        return new ChatResponseDto
                        {
                            Success = false,
                            OutOfQuota = true,
                            Remaining = remainingBefore,
                            Message = effectivePlan == "Free"
                                ? $"Bạn đã dùng hết {limit} câu hỏi miễn phí trong tháng này. Nâng cấp gói để tiếp tục!"
                                : $"Bạn đã đạt giới hạn {limit} câu hỏi/tháng của gói {effectivePlan}."
                        };
                    }
                }

                var citations = new List<CitationDto>();
                var contextText = string.Empty;

                if (request.SelectedDocIds != null && request.SelectedDocIds.Any())
                {
                    var questionEmbedding = await _geminiService.GetEmbeddingAsync(request.Message);
                    var similarChunks = await _documentRepository.SearchSimilarChunksAsync(
                        new Vector(questionEmbedding),
                        request.SelectedDocIds,
                        topK: 20); // Top-K Retrieval

                    if (similarChunks.Any())
                    {
                        // Thuc hien Re-ranking de chon ra 5 chunk tot nhat bang LLM
                        similarChunks = await RerankChunksAsync(request.Message, similarChunks, topN: 5);

                        contextText = string.Join(
                            "\n\n",
                            similarChunks.Select((chunk, index) =>
                                $"Nguồn {index + 1}:\n" +
                                $"Tài liệu: {chunk.Document.Title}\n" +
                                $"Môn: {chunk.Document.Subject?.Name ?? "Không rõ"}\n" +
                                $"Chương: {chunk.Document.Chapter?.Title ?? "Không rõ"}\n" +
                                $"Đoạn: {chunk.OrderIndex}\n" +
                                $"Nội dung: {chunk.Content}"));

                        citations = similarChunks
                            .Select(chunk => new CitationDto
                            {
                                DocumentId = chunk.DocumentId,
                                DocumentTitle = chunk.Document.Title,
                                SubjectName = chunk.Document.Subject?.Name,
                                ChapterTitle = chunk.Document.Chapter?.Title,
                                ChunkOrderIndex = chunk.OrderIndex,
                                Snippet = BuildSnippet(chunk.Content),
                                FullContent = StripMarkdown(chunk.Content ?? "")
                            })
                            .ToList();
                    }
                    else
                    {
                        var docs = await _documentRepository.GetDocumentsByIdsAsync(request.SelectedDocIds);
                        foreach (var doc in docs)
                        {
                            var snippet = BuildSnippet(doc.Content);
                            var fullCleaned = StripMarkdown(doc.Content ?? "");
                            var fullContent = fullCleaned.Length > 3000 ? fullCleaned[..3000] + "...\n(Tài liệu quá dài, vui lòng xem bản đầy đủ)" : fullCleaned;
                            contextText += $"Tài liệu: {doc.Title}\nNội dung: {snippet}\n\n";
                            citations.Add(new CitationDto
                            {
                                DocumentId = doc.Id,
                                DocumentTitle = doc.Title,
                                ChunkOrderIndex = 0,
                                Snippet = snippet,
                                FullContent = fullContent
                            });
                        }
                    }
                }

                if (request.RestrictToDocs)
                {
                    if (request.SelectedDocIds == null || !request.SelectedDocIds.Any())
                    {
                        return new ChatResponseDto
                        {
                            Success = false,
                            Message = "Hãy chọn ít nhất một tài liệu trước khi hỏi trong chế độ giơi hạn theo tài liệu."
                        };
                    }

                    if (string.IsNullOrWhiteSpace(contextText))
                    {
                        return new ChatResponseDto
                        {
                            Success = false,
                            Message = "Tôi không tìm thấy đoạn tài liệu phù hợp để trả lời câu hỏi này trong các tài liệu đã chọn."
                        };
                    }
                }

                var prompt = BuildPrompt(request.Message, conversationHistory, contextText, request.RestrictToDocs, effectivePlan, remainingAfter);
                var replyText = await _geminiService.GenerateAnswerAsync(prompt, request.ModelName);

                if (string.IsNullOrWhiteSpace(replyText))
                {
                    return new ChatResponseDto
                    {
                        Success = false,
                        Message = "AI không trả về nội dung hợp lệ."
                    };
                }

                var session = existingSession ?? await ResolveSessionAsync(userId, request.SessionId, request.Message, createIfMissing: true);
                if (session == null)
                {
                    return new ChatResponseDto
                    {
                        Success = false,
                        Message = "Không thể tạo hoặc tìm thấy phiên chat."
                    };
                }

                var title = session.Title;
                if (string.IsNullOrWhiteSpace(title) || title == DefaultSessionTitle)
                {
                    title = BuildSessionTitle(request.Message);
                }

                await _chatRepository.AddMessageAsync(new ChatMessage
                {
                    ChatSessionId = session.Id,
                    Role = "user",
                    Text = request.Message,
                    Timestamp = DateTime.UtcNow
                });

                await _chatRepository.AddMessageAsync(new ChatMessage
                {
                    ChatSessionId = session.Id,
                    Role = "model",
                    Text = replyText,
                    CitationPayloadJson = SerializeCitations(citations),
                    Timestamp = DateTime.UtcNow
                });

                if (title != session.Title)
                {
                    await _chatRepository.UpdateSessionTitleAsync(session.Id, title);
                }

                if (user != null)
                {
                    user.MonthlyQuestionCount++;
                    await _userRepository.UpdateUserAsync(user);
                }

                return new ChatResponseDto
                {
                    Success = true,
                    Reply = replyText,
                    Remaining = remainingAfter,
                    SessionId = session.Id,
                    SessionTitle = title,
                    Citations = citations
                };
            }
            catch (Exception ex)
            {
                return new ChatResponseDto
                {
                    Success = false,
                    Message = "Lỗi hệ thống: " + ex.Message
                };
            }
        }

        private async Task<ChatSession?> ResolveSessionAsync(int userId, int? sessionId, string message, bool createIfMissing)
        {
            if (sessionId.HasValue && sessionId.Value > 0)
            {
                var existing = await _chatRepository.GetSessionByIdAsync(sessionId.Value);
                if (existing != null && existing.UserId == userId)
                {
                    return existing;
                }
            }

            if (!createIfMissing)
            {
                return null;
            }

            var title = BuildSessionTitle(message);
            return await _chatRepository.CreateSessionAsync(userId, title);
        }

        private static string BuildSessionTitle(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return DefaultSessionTitle;
            }

            return message.Length > 22 ? message[..22] + "..." : message;
        }

        private static ChatSessionDto MapSession(ChatSession session)
        {
            return new ChatSessionDto
            {
                Id = session.Id,
                Title = session.Title,
                CreatedAt = session.CreatedAt,
                Messages = session.Messages
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new ChatMessageDto
                    {
                        Role = m.Role,
                        Text = m.Text,
                        Timestamp = m.Timestamp,
                        Citations = DeserializeCitations(m.CitationPayloadJson)
                    })
                    .ToList()
            };
        }

        private static string BuildSnippet(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            var cleaned = StripMarkdown(content);
            var normalized = cleaned.Replace("\r", " ").Replace("\n", " ").Trim();
            // Loại bỏ các khoảng trắng thừa liên tiếp phát sinh sau khi làm sạch
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s{2,}", " ").Trim();
            return normalized.Length > 220 ? normalized[..220] + "..." : normalized;
        }

        /// <summary>
        /// Làm sạch các ký hiệu Markdown (in đậm, in nghiêng, tiêu đề, mã, liên kết...) 
        /// để hiển thị text thuần túy trong tooltip trích dẫn.
        /// </summary>
        private static string StripMarkdown(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            static string R(string input, string pattern, string replacement)
                => System.Text.RegularExpressions.Regex.Replace(input, pattern, replacement);
            static string RM(string input, string pattern, string replacement)
                => System.Text.RegularExpressions.Regex.Replace(input, pattern, replacement,
                   System.Text.RegularExpressions.RegexOptions.Multiline);

            // Xóa các tiêu đề (## Heading, # Heading)
            text = RM(text, @"^#{1,6}\s+", "");

            // Xóa bold+italic kết hợp (***text*** hoặc ___text___)
            text = R(text, @"\*{3}(.+?)\*{3}", "$1");
            text = R(text, @"_{3}(.+?)_{3}", "$1");

            // Xóa bold (**text** hoặc __text__)
            text = R(text, @"\*{2}(.+?)\*{2}", "$1");
            text = R(text, @"_{2}(.+?)_{2}", "$1");

            // Xóa italic (*text* hoặc _text_)
            text = R(text, @"\*(.+?)\*", "$1");
            text = R(text, @"_(.+?)_", "$1");

            // Xóa inline code (`code`)
            text = R(text, @"`(.+?)`", "$1");

            // Xóa code block (```...```)
            text = R(text, @"```[\s\S]*?```", "");

            // Xóa hình ảnh (![alt](url))
            text = R(text, @"!\[.*?\]\(.*?\)", "");

            // Đổi liên kết [text](url) → text
            text = R(text, @"\[(.+?)\]\(.*?\)", "$1");

            // Xóa dấu gạch ngang (--- hoặc ***)
            text = RM(text, @"^[-*_]{3,}\s*$", "");

            // Xóa đánh dấu trang (ví dụ: -----Trang 1------, --- Trang 1 ---)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"-{2,}\s*Trang\s+\d+\s*-{2,}", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Xóa ký hiệu danh sách (- item, * item, 1. item)
            text = RM(text, @"^[\s]*[-*+]\s+", "");
            text = RM(text, @"^[\s]*\d+\.\s+", "");

            // Xóa blockquote (> text)
            text = RM(text, @"^>\s?", "");

            return text;
        }

        private static string? SerializeCitations(List<CitationDto> citations)
        {
            if (citations == null || citations.Count == 0)
            {
                return null;
            }

            return JsonSerializer.Serialize(citations);
        }

        private static List<CitationDto> DeserializeCitations(string? citationPayloadJson)
        {
            if (string.IsNullOrWhiteSpace(citationPayloadJson))
            {
                return new List<CitationDto>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<CitationDto>>(citationPayloadJson) ?? new List<CitationDto>();
            }
            catch
            {
                return new List<CitationDto>();
            }
        }

        private static string BuildConversationHistory(IEnumerable<ChatMessage>? messages, int maxMessages = 8)
        {
            if (messages == null)
            {
                return string.Empty;
            }

            var recentMessages = messages
                .OrderBy(m => m.Timestamp)
                .TakeLast(maxMessages)
                .Select(m =>
                {
                    var roleLabel = m.Role == "user" ? "Người dùng" : "Trợ lý AI";
                    return $"{roleLabel}: {m.Text}";
                })
                .ToList();

            return recentMessages.Count == 0
                ? string.Empty
                : string.Join("\n", recentMessages);
        }

        private static string BuildPrompt(
            string message,
            string conversationHistory,
            string contextText,
            bool restrictToDocs,
            string planName,
            int remainingQueries)
        {
            var promptSections = new List<string>();

            // Thêm thông tin hệ thống về gói cước và số lượt hỏi còn lại
            if (remainingQueries != int.MaxValue)
            {
                promptSections.Add(
                    $"[THÔNG TIN HỆ THỐNG]\nNgười dùng đang sử dụng gói: {planName}.\nSố lượt hỏi còn lại trong tháng sau câu hỏi này: {remainingQueries} lượt.\n(Nếu người dùng hỏi về số lượt còn lại, hoặc liên quan đến giới hạn, hãy dùng thông tin này để trả lời hoặc nhắc nhở. Không cần nhắc đến nếu không liên quan).");
            }
            else
            {
                promptSections.Add(
                    $"[THÔNG TIN HỆ THỐNG]\nNgười dùng đang sử dụng gói: {planName} (Không giới hạn số lượt hỏi).");
            }

            if (!string.IsNullOrWhiteSpace(conversationHistory))
            {
                promptSections.Add(
                    "Lịch sử hội thoại gần đây:\n" +
                    conversationHistory +
                    "\n\nHãy giữ đúng ngữ cảnh hội thoại khi trả lời câu hỏi mới.");
            }

            if (!string.IsNullOrWhiteSpace(contextText))
            {
                var citationInstruction = "\n\nQUAN TRỌNG: Bắt buộc phải trích dẫn nguồn cho các câu văn có sử dụng thông tin từ tài liệu. Khi bạn viết một câu lấy thông tin từ 'Nguồn X', hãy BẮT BUỘC chèn [X] vào ngay cuối câu đó. Ví dụ: 'Trái đất hình tròn [1][2].' KHÔNG liệt kê lại danh sách nguồn ở cuối câu trả lời, chỉ cần đánh dấu [X] trong đoạn văn.";

                if (restrictToDocs)
                {
                    promptSections.Add(
                        "Tài liệu liên quan:\n" +
                        contextText +
                        "\n\nChỉ sử dụng thông tin trong tài liệu trên để trả lời. Nếu tài liệu không đủ thông tin, hãy nói rõ ràng." + citationInstruction);
                }
                else
                {
                    promptSections.Add(
                        "Tài liệu liên quan (có thể tham khảo):\n" +
                        contextText +
                        "\n\nHãy ưu tiên sử dụng thông tin trong tài liệu này. Nếu tài liệu không đủ thông tin, bạn có thể sử dụng kiến thức sẵn có của bạn để trả lời." + citationInstruction);
                }
            }

            promptSections.Add($"Câu hỏi hiện tại: {message}");
            return string.Join("\n\n", promptSections);
        }

        private async Task<List<DocumentChunk>> RerankChunksAsync(string query, List<DocumentChunk> chunks, int topN)
        {
            if (chunks.Count <= topN)
            {
                return chunks;
            }

            var promptSections = new List<string>
            {
                "Bạn là một hệ thống chấm điểm mức độ liên quan của tài liệu. Nhiệm vụ của bạn là chọn ra các đoạn tài liệu phù hợp nhất với câu hỏi.",
                $"Câu hỏi: {query}",
                "Danh sách các đoạn tài liệu:"
            };

            for (int i = 0; i < chunks.Count; i++)
            {
                promptSections.Add($"[{i}] {chunks[i].Content}");
            }

            promptSections.Add($@"Vui lòng trả về MẢNG JSON gồm tối đa {topN} chỉ số (index) của các đoạn tài liệu liên quan nhất đến câu hỏi, sắp xếp theo mức độ phù hợp giảm dần. 
Ví dụ: [3, 0, 1, 5, 2]
CHỈ TRẢ VỀ MẢNG JSON, KHÔNG GIẢI THÍCH HOẶC THÊM BẤT KỲ VĂN BẢN NÀO KHÁC.");

            var prompt = string.Join("\n\n", promptSections);
            
            try
            {
                // Sử dụng gemini-1.5-flash cho nhiệm vụ re-rank để đảm bảo tốc độ
                var reply = await _geminiService.GenerateAnswerAsync(prompt, "gemini-1.5-flash");
                
                // Thử trích xuất JSON array từ phản hồi
                var jsonStart = reply.IndexOf('[');
                var jsonEnd = reply.LastIndexOf(']');
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonStr = reply.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var indices = JsonSerializer.Deserialize<List<int>>(jsonStr);
                    if (indices != null && indices.Count > 0)
                    {
                        var reranked = new List<DocumentChunk>();
                        var addedIndices = new HashSet<int>();
                        foreach (var idx in indices)
                        {
                            if (idx >= 0 && idx < chunks.Count && !addedIndices.Contains(idx))
                            {
                                reranked.Add(chunks[idx]);
                                addedIndices.Add(idx);
                            }
                        }
                        
                        // Nếu thiếu số lượng thì bổ sung từ ban đầu
                        if (reranked.Count < topN)
                        {
                            var remaining = chunks.Where((c, i) => !addedIndices.Contains(i)).Take(topN - reranked.Count);
                            reranked.AddRange(remaining);
                        }
                        return reranked;
                    }
                }
            }
            catch
            {
                // Fallback: nếu lỗi thì trả về topN ban đầu
            }

            return chunks.Take(topN).ToList();
        }
    }
}
