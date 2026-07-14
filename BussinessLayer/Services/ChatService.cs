using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using DataAccessLayer.IRepositories;
using Microsoft.Extensions.Caching.Memory;
using Pgvector;

using BussinessLayer.IServices;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Dịch vụ Trò chuyện AI (Chatbot). Quản lý phiên chat, gửi câu hỏi đến Gemini API, thực hiện tìm kiếm ngữ nghĩa (Semantic Search) trên tài liệu đã Embedding, và trả về câu trả lời kèm trích dẫn nguồn (Citation).
    /// </summary>
    public class ChatService : IChatService
    {
        private const string DefaultSessionTitle = "Cuộc trò chuyện mới";
        private const int MaxMessageLength = 8000; // giới hạn độ dài tin nhắn (ký tự)

        private readonly IChatRepository _chatRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IGeminiService _geminiService;
        private readonly IUserRepository _userRepository;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IMemoryCache _cache;

        public static int GetShortTermLimit(string plan) => plan switch
        {
            "Basic" => 10,
            "Pro" => 20,
            "Ultra" => int.MaxValue,
            _ => 10
        };

        public static int GetMonthlyLimit(string plan) => plan switch
        {
            "Basic" => 50,
            "Pro" => 500,
            "Ultra" => int.MaxValue,
            _ => 50
        };

        public ChatService(
            IChatRepository chatRepository,
            IDocumentRepository documentRepository,
            IGeminiService geminiService,
            IUserRepository userRepository,
            ISubscriptionService subscriptionService,
            IMemoryCache cache)
        {
            _chatRepository = chatRepository;
            _documentRepository = documentRepository;
            _geminiService = geminiService;
            _userRepository = userRepository;
            _subscriptionService = subscriptionService;
            _cache = cache;
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
            if (session == null || session.UserId != userId) return false;
            await _chatRepository.DeleteSessionAsync(sessionId);
            return true;
        }

        public async Task<bool> ClearSessionAsync(int userId, int sessionId)
        {
            var session = await _chatRepository.GetSessionByIdAsync(sessionId);
            if (session == null || session.UserId != userId) return false;
            await _chatRepository.ClearSessionAsync(sessionId);
            return true;
        }

        public async Task<List<ChatMessageDto>> GetSessionMessagesPagedAsync(int userId, int sessionId, int page, int pageSize = 20)
        {
            var session = await _chatRepository.GetSessionByIdAsync(sessionId);
            if (session == null || session.UserId != userId) return new List<ChatMessageDto>();
            var messages = await _chatRepository.GetMessagesPagedAsync(sessionId, page, pageSize);
            return messages.Select(m => new ChatMessageDto
            {
                Role = m.Role,
                Text = m.Text,
                Timestamp = m.Timestamp,
                Citations = DeserializeCitations(m.CitationPayloadJson)
            }).ToList();
        }

        // ─── ProcessChatMessage (blocking, giữ nguyên cho fallback) ────────────
        public async Task<ChatResponseDto> ProcessChatMessageAsync(int userId, ChatRequestDto request)
        {
            try
            {
                // Validate độ dài tin nhắn
                if (request.Message.Length > MaxMessageLength)
                {
                    return new ChatResponseDto
                    {
                        Success = false,
                        Message = $"Tin nhắn quá dài ({request.Message.Length} ký tự). Giới hạn là {MaxMessageLength} ký tự."
                    };
                }

                var existingSession = await ResolveSessionAsync(userId, request.SessionId, request.Message, createIfMissing: false);
                var conversationHistory = BuildConversationHistory(existingSession?.Messages);

                var (quotaResult, user, effectivePlan, remainingShortAfter, remainingMonthAfter, remainingAfter, isUsingExtraQuota)
                    = await CheckQuotaAsync(userId);

                if (!quotaResult.Success)
                    return quotaResult;

                var (contextText, citations) = await BuildContextAsync(request);

                if (request.RestrictToDocs)
                {
                    if (request.SelectedDocIds == null || !request.SelectedDocIds.Any())
                        return new ChatResponseDto { Success = false, Message = "Hãy chọn ít nhất một tài liệu trước khi hỏi trong chế độ hạn chế theo tài liệu." };
                    if (string.IsNullOrWhiteSpace(contextText))
                        return new ChatResponseDto { Success = false, Message = "Tôi không tìm thấy đoạn tài liệu phù hợp để trả lời câu hỏi này trong các tài liệu đã chọn." };
                }

                var prompt = BuildPrompt(request.Message, conversationHistory, contextText, request.RestrictToDocs, effectivePlan, remainingShortAfter, remainingMonthAfter);

                string replyText;
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                    replyText = await _geminiService.GenerateAnswerAsync(prompt, request.ModelName);
                }
                catch (OperationCanceledException)
                {
                    return new ChatResponseDto { Success = false, Message = "Yêu cầu AI vượt quá 60 giây, vui lòng thử lại sau." };
                }

                if (string.IsNullOrWhiteSpace(replyText))
                    return new ChatResponseDto { Success = false, Message = "AI không trả về nội dung hợp lệ." };

                if (replyText.StartsWith("⚠️") || replyText.StartsWith("Lỗi khi gọi AI"))
                    return new ChatResponseDto { Success = false, Message = replyText };

                var session = existingSession ?? await ResolveSessionAsync(userId, request.SessionId, request.Message, createIfMissing: true);
                if (session == null)
                    return new ChatResponseDto { Success = false, Message = "Không thể tạo hoặc tìm thấy phiên chat." };

                await SaveMessagesAndUpdateSessionAsync(session, request.Message, replyText, citations);
                await UpdateQuotaAsync(user, isUsingExtraQuota);

                return new ChatResponseDto
                {
                    Success = true,
                    Reply = replyText,
                    Remaining = remainingAfter,
                    SessionId = session.Id,
                    SessionTitle = session.Title,
                    Citations = citations
                };
            }
            catch (Exception ex)
            {
                return new ChatResponseDto { Success = false, Message = "Lỗi hệ thống: " + ex.Message };
            }
        }

        // ─── ProcessStreamingChatMessage ──────────────────────────────────────
        public async Task<ChatResponseDto> ProcessStreamingChatMessageAsync(
            int userId,
            ChatRequestDto request,
            Func<string, Task> onChunk,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request.Message.Length > MaxMessageLength)
                    return new ChatResponseDto
                    {
                        Success = false,
                        Message = $"Tin nhắn quá dài ({request.Message.Length} ký tự). Giới hạn là {MaxMessageLength} ký tự."
                    };

                var existingSession = await ResolveSessionAsync(userId, request.SessionId, request.Message, createIfMissing: false);
                var conversationHistory = BuildConversationHistory(existingSession?.Messages);

                var (quotaResult, user, effectivePlan, remainingShortAfter, remainingMonthAfter, remainingAfter, isUsingExtraQuota)
                    = await CheckQuotaAsync(userId);

                if (!quotaResult.Success)
                    return quotaResult;

                var (contextText, citations) = await BuildContextAsync(request);

                if (request.RestrictToDocs)
                {
                    if (request.SelectedDocIds == null || !request.SelectedDocIds.Any())
                        return new ChatResponseDto { Success = false, Message = "Hãy chọn ít nhất một tài liệu trước khi hỏi trong chế độ hạn chế theo tài liệu." };
                    if (string.IsNullOrWhiteSpace(contextText))
                        return new ChatResponseDto { Success = false, Message = "Tôi không tìm thấy đoạn tài liệu phù hợp để trả lời câu hỏi này." };
                }

                var prompt = BuildPrompt(request.Message, conversationHistory, contextText, request.RestrictToDocs, effectivePlan, remainingShortAfter, remainingMonthAfter);

                // Stream từng chunk
                var fullReply = new StringBuilder();
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(90));

                await foreach (var chunk in _geminiService.GenerateStreamingAnswerAsync(prompt, request.ModelName, timeoutCts.Token))
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    fullReply.Append(chunk);
                    await onChunk(chunk);
                }

                var replyText = fullReply.ToString();
                if (string.IsNullOrWhiteSpace(replyText))
                    return new ChatResponseDto { Success = false, Message = "AI không trả về nội dung hợp lệ." };

                if (replyText.StartsWith("⚠️") || replyText.StartsWith("Lỗi khi gọi AI"))
                    return new ChatResponseDto { Success = false, Message = replyText };

                var session = existingSession ?? await ResolveSessionAsync(userId, request.SessionId, request.Message, createIfMissing: true);
                if (session == null)
                    return new ChatResponseDto { Success = false, Message = "Không thể tạo hoặc tìm thấy phiên chat." };

                await SaveMessagesAndUpdateSessionAsync(session, request.Message, replyText, citations);
                await UpdateQuotaAsync(user, isUsingExtraQuota);

                return new ChatResponseDto
                {
                    Success = true,
                    Reply = replyText,
                    Remaining = remainingAfter,
                    SessionId = session.Id,
                    SessionTitle = session.Title,
                    Citations = citations
                };
            }
            catch (OperationCanceledException)
            {
                return new ChatResponseDto { Success = false, Message = "Yêu cầu AI hết thời gian chờ (90 giây), vui lòng thử lại." };
            }
            catch (Exception ex)
            {
                return new ChatResponseDto { Success = false, Message = "Lỗi hệ thống: " + ex.Message };
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private async Task<(ChatResponseDto quotaResult, User? user, string effectivePlan,
            int remainingShortAfter, int remainingMonthAfter, int remainingAfter, bool isUsingExtraQuota)>
            CheckQuotaAsync(int userId)
        {
            var effectivePlan = "Basic";
            var remainingShortAfter = int.MaxValue;
            var remainingMonthAfter = int.MaxValue;
            var remainingAfter = int.MaxValue;
            var isUsingExtraQuota = false;
            User? user = null;

            user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return (new ChatResponseDto { Success = true }, null, effectivePlan, remainingShortAfter, remainingMonthAfter, remainingAfter, false);

            await _subscriptionService.CheckAndUpdateQuotaAsync(userId);
            user = await _userRepository.GetUserByIdAsync(userId) ?? user;

            var now = DateTime.UtcNow;
            var planActive = user.SubscriptionPlan == "Basic" || user.SubscriptionPlan == "Free" ||
                             (user.SubscriptionExpiry.HasValue && user.SubscriptionExpiry.Value >= now);
            effectivePlan = planActive ? user.SubscriptionPlan : "Basic";
            if (effectivePlan == "Free") effectivePlan = "Basic";

            var limitShort = GetShortTermLimit(effectivePlan);
            var limitMonth = GetMonthlyLimit(effectivePlan);

            var remainingShortBefore = limitShort == int.MaxValue ? int.MaxValue : Math.Max(0, limitShort - user.ShortTermQuestionCount);
            remainingShortAfter = remainingShortBefore == int.MaxValue ? int.MaxValue : Math.Max(0, remainingShortBefore - 1);
            var remainingMonthBefore = limitMonth == int.MaxValue ? int.MaxValue : Math.Max(0, limitMonth - user.MonthlyQuestionCount);
            remainingMonthAfter = remainingMonthBefore == int.MaxValue ? int.MaxValue : Math.Max(0, remainingMonthBefore - 1);
            remainingAfter = remainingShortAfter;

            bool outOfStandardQuota = false;
            string outOfQuotaMessage = "";

            if (limitMonth != int.MaxValue && user.MonthlyQuestionCount >= limitMonth)
            {
                outOfStandardQuota = true;
                outOfQuotaMessage = $"Bạn đã dùng hết {limitMonth} câu hỏi trong tháng này. Vui lòng chờ sang tháng sau hoặc nâng cấp gói.";
            }
            else if (limitShort != int.MaxValue && user.ShortTermQuestionCount >= limitShort)
            {
                outOfStandardQuota = true;
                var remainingTime = user.ShortTermResetDate.HasValue ? (user.ShortTermResetDate.Value - now) : TimeSpan.Zero;
                var hours = Math.Max(0, (int)Math.Ceiling(remainingTime.TotalHours));
                outOfQuotaMessage = effectivePlan == "Basic"
                    ? $"Bạn đã đạt giới hạn {limitShort} câu hỏi trong 5 giờ. Vui lòng chờ thêm {hours} giờ."
                    : $"Bạn đã đạt giới hạn {limitShort} câu hỏi trong 5 giờ của gói {effectivePlan}. Vui lòng chờ thêm {hours} giờ.";
            }

            if (outOfStandardQuota)
            {
                if (user.UseExtraQuota && user.ExtraQuestionQuota > 0)
                {
                    isUsingExtraQuota = true;
                    remainingAfter = user.ExtraQuestionQuota - 1;
                }
                else
                {
                    if (user.ExtraQuestionQuota > 0)
                        outOfQuotaMessage += " Bạn vẫn còn lượt hỏi dự phòng, hãy BẬT công tắc 'Sử dụng lượt dự phòng' để tiếp tục.";
                    else
                        outOfQuotaMessage += " Bạn đã hết lượt hỏi. Vui lòng vào mục Gói Hội Viên để mua thêm lượt dự phòng.";

                    return (new ChatResponseDto { Success = false, OutOfQuota = true, Remaining = remainingShortBefore, Message = outOfQuotaMessage },
                        user, effectivePlan, remainingShortAfter, remainingMonthAfter, remainingAfter, isUsingExtraQuota);
                }
            }

            return (new ChatResponseDto { Success = true }, user, effectivePlan, remainingShortAfter, remainingMonthAfter, remainingAfter, isUsingExtraQuota);
        }

        private async Task<(string contextText, List<CitationDto> citations)> BuildContextAsync(ChatRequestDto request)
        {
            var citations = new List<CitationDto>();
            var contextText = string.Empty;

            if (request.SelectedDocIds == null || !request.SelectedDocIds.Any())
                return (contextText, citations);

            // Cache key = sorted doc IDs + question hash
            var cacheKey = $"chunks_{string.Join(",", request.SelectedDocIds.OrderBy(x => x))}_{request.Message.GetHashCode()}";
            if (_cache.TryGetValue(cacheKey, out (string ctx, List<CitationDto> cit) cached))
                return (cached.ctx, cached.cit);

            List<DocumentChunk> similarChunks;
            try
            {
                var questionEmbedding = await _geminiService.GetEmbeddingAsync(request.Message);
                similarChunks = await _documentRepository.SearchSimilarChunksAsync(
                    new Vector(questionEmbedding), request.SelectedDocIds, topK: 20);
            }
            catch
            {
                // Embedding bị lỗi (vd 429 hết quota) → không semantic search được,
                // fallback dùng nội dung tài liệu trực tiếp thay vì làm hỏng cả câu chat.
                similarChunks = new List<DocumentChunk>();
            }

            if (similarChunks.Any())
            {
                similarChunks = await RerankChunksAsync(request.Message, similarChunks, topN: 5);
                contextText = string.Join("\n\n", similarChunks.Select((chunk, index) =>
                    $"Nguồn {index + 1}:\n" +
                    $"Tài liệu: {chunk.Document.Title}\n" +
                    $"Môn: {chunk.Document.Subject?.Name ?? "Không rõ"}\n" +
                    $"Chương: {chunk.Document.Chapter?.Title ?? "Không rõ"}\n" +
                    $"Đoạn: {chunk.OrderIndex}\n" +
                    $"Nội dung: {chunk.Content}"));

                citations = similarChunks.Select(chunk => new CitationDto
                {
                    DocumentId = chunk.DocumentId,
                    DocumentTitle = chunk.Document.Title,
                    SubjectName = chunk.Document.Subject?.Name,
                    ChapterTitle = chunk.Document.Chapter?.Title,
                    ChunkOrderIndex = chunk.OrderIndex,
                    Snippet = BuildSnippet(chunk.Content),
                    FullContent = StripMarkdown(chunk.Content ?? "")
                }).ToList();
            }
            else
            {
                var docs = await _documentRepository.GetDocumentsByIdsAsync(request.SelectedDocIds);
                foreach (var doc in docs)
                {
                    var snippet = BuildSnippet(doc.Content);
                    var fullCleaned = StripMarkdown(doc.Content ?? "");
                    var fullContent = fullCleaned.Length > 3000 ? fullCleaned[..3000] + "...\n(Tài liệu quá dài)" : fullCleaned;
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

            // Cache 5 phut
            _cache.Set(cacheKey, (contextText, citations), TimeSpan.FromMinutes(5));
            return (contextText, citations);
        }

        private async Task SaveMessagesAndUpdateSessionAsync(
            ChatSession session, string userMessage, string replyText, List<CitationDto> citations)
        {
            var title = session.Title;
            if (string.IsNullOrWhiteSpace(title) || title == DefaultSessionTitle)
                title = BuildSessionTitle(userMessage);

            await _chatRepository.AddMessageAsync(new ChatMessage
            {
                ChatSessionId = session.Id,
                Role = "user",
                MessageType = "user",
                Text = userMessage,
                TokenCount = EstimateTokenCount(userMessage),
                Timestamp = DateTime.UtcNow
            });

            await _chatRepository.AddMessageAsync(new ChatMessage
            {
                ChatSessionId = session.Id,
                Role = "model",
                MessageType = "model",
                Text = replyText,
                CitationPayloadJson = SerializeCitations(citations),
                TokenCount = EstimateTokenCount(replyText),
                Timestamp = DateTime.UtcNow
            });

            if (title != session.Title)
                await _chatRepository.UpdateSessionTitleAsync(session.Id, title);
            else
                await _chatRepository.UpdateSessionUpdatedAtAsync(session.Id);
        }

        private async Task UpdateQuotaAsync(User? user, bool isUsingExtraQuota)
        {
            if (user == null) return;
            if (isUsingExtraQuota)
                user.ExtraQuestionQuota--;
            else
            {
                user.ShortTermQuestionCount++;
                user.MonthlyQuestionCount++;
            }
            await _userRepository.UpdateUserAsync(user);
        }

        private async Task<ChatSession?> ResolveSessionAsync(int userId, int? sessionId, string message, bool createIfMissing)
        {
            if (sessionId.HasValue && sessionId.Value > 0)
            {
                var existing = await _chatRepository.GetSessionByIdAsync(sessionId.Value);
                if (existing != null && existing.UserId == userId) return existing;
            }
            if (!createIfMissing) return null;
            var title = BuildSessionTitle(message);
            return await _chatRepository.CreateSessionAsync(userId, title);
        }

        private static string BuildSessionTitle(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return DefaultSessionTitle;
            return message.Length > 22 ? message[..22] + "..." : message;
        }

        private static int EstimateTokenCount(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return (int)Math.Ceiling(text.Length / 4.0); // rough estimate: 4 chars ~ 1 token
        }

        private static ChatSessionDto MapSession(ChatSession session)
        {
            return new ChatSessionDto
            {
                Id = session.Id,
                Title = session.Title,
                CreatedAt = session.CreatedAt,
                Messages = (session.Messages ?? new List<ChatMessage>())
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
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;
            var cleaned = StripMarkdown(content);
            var normalized = cleaned.Replace("\r", " ").Replace("\n", " ").Trim();
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s{2,}", " ").Trim();
            return normalized.Length > 220 ? normalized[..220] + "..." : normalized;
        }

        private static string StripMarkdown(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            static string R(string input, string pattern, string replacement)
                => System.Text.RegularExpressions.Regex.Replace(input, pattern, replacement);
            static string RM(string input, string pattern, string replacement)
                => System.Text.RegularExpressions.Regex.Replace(input, pattern, replacement,
                   System.Text.RegularExpressions.RegexOptions.Multiline);
            text = RM(text, @"^#{1,6}\s+", "");
            text = R(text, @"\*{3}(.+?)\*{3}", "$1");
            text = R(text, @"_{3}(.+?)_{3}", "$1");
            text = R(text, @"\*{2}(.+?)\*{2}", "$1");
            text = R(text, @"_{2}(.+?)_{2}", "$1");
            text = R(text, @"\*(.+?)\*", "$1");
            text = R(text, @"_(.+?)_", "$1");
            text = R(text, @"`(.+?)`", "$1");
            text = R(text, @"```[\s\S]*?```", "");
            text = R(text, @"!\[.*?\]\(.*?\)", "");
            text = R(text, @"\[(.+?)\]\(.*?\)", "$1");
            text = RM(text, @"^[-*_]{3,}\s*$", "");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"-{2,}\s*Trang\s+\d+\s*-{2,}", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = RM(text, @"^[\s]*[-*+]\s+", "");
            text = RM(text, @"^[\s]*\d+\.\s+", "");
            text = RM(text, @"^>\s?", "");
            return text;
        }

        private static string? SerializeCitations(List<CitationDto> citations)
        {
            if (citations == null || citations.Count == 0) return null;
            return JsonSerializer.Serialize(citations);
        }

        private static List<CitationDto> DeserializeCitations(string? citationPayloadJson)
        {
            if (string.IsNullOrWhiteSpace(citationPayloadJson)) return new List<CitationDto>();
            try { return JsonSerializer.Deserialize<List<CitationDto>>(citationPayloadJson) ?? new List<CitationDto>(); }
            catch { return new List<CitationDto>(); }
        }

        private static string BuildConversationHistory(IEnumerable<ChatMessage>? messages, int maxMessages = 8)
        {
            if (messages == null) return string.Empty;
            var recentMessages = messages
                .OrderBy(m => m.Timestamp)
                .TakeLast(maxMessages)
                .Select(m =>
                {
                    var roleLabel = m.Role == "user" ? "Người dùng" : "Trợ lý AI";
                    // Truncate dài để tránh context quá lớn
                    var text = m.Text.Length > 500 ? m.Text[..500] + "..." : m.Text;
                    return $"{roleLabel}: {text}";
                }).ToList();
            return recentMessages.Count == 0 ? string.Empty : string.Join("\n", recentMessages);
        }

        private static string BuildPrompt(
            string message,
            string conversationHistory,
            string contextText,
            bool restrictToDocs,
            string planName,
            int remainingShortQueries,
            int remainingMonthQueries)
        {
            var promptSections = new List<string>();

            if (remainingShortQueries != int.MaxValue || remainingMonthQueries != int.MaxValue)
            {
                var shortText = remainingShortQueries != int.MaxValue ? $"còn {remainingShortQueries} lượt trong chu kỳ 5 giờ" : "không giới hạn trong 5 giờ";
                var monthText = remainingMonthQueries != int.MaxValue ? $"còn {remainingMonthQueries} lượt trong tháng" : "không giới hạn trong tháng";
                promptSections.Add($"[THÔNG TIN HỆ THỐNG]\nNgười dùng đang sử dụng gói: {planName}.\nSố lượt hỏi còn lại sau câu hỏi này: {shortText}, và {monthText}.\n(Nếu người dùng hỏi về số lượt còn lại, hãy dùng thông tin này để trả lời. Không cần nhắc đến nếu không liên quan).");
            }
            else
            {
                promptSections.Add($"[THÔNG TIN HỆ THỐNG]\nNgười dùng đang sử dụng gói: {planName} (Không giới hạn số lượt hỏi).");
            }

            promptSections.Add("[QUAN TRỌNG VỀ DANH TÍNH CỦA BẠN]\nNếu người dùng hỏi bạn là ai, bạn được tạo ra bởi ai, hoặc bạn đang sử dụng mô hình/ngôn ngữ/AI nào (ví dụ: GPT, Gemini, Claude, LLaMA, v.v.), hãy TUYỆT ĐỐI KHÔNG tiết lộ mô hình thực sự của bạn. Thay vào đó, hãy trả lời một cách lịch sự rằng: 'Tôi là trợ lý AI được phát triển và tích hợp độc quyền bởi hệ thống ChatEdu để hỗ trợ bạn trong học tập.'");

            if (!string.IsNullOrWhiteSpace(conversationHistory))
            {
                promptSections.Add("Lịch sử hội thoại gần đây:\n" + conversationHistory + "\n\nHãy giữ đúng ngữ cảnh hội thoại khi trả lời câu hỏi mới.");
            }

            if (!string.IsNullOrWhiteSpace(contextText))
            {
                var citationInstruction = "\n\nQUAN TRỌNG: Bắt buộc phải trích dẫn nguồn cho các câu văn có sử dụng thông tin từ tài liệu. Khi bạn viết một câu lấy thông tin từ 'Nguồn X', hãy BẮT BUỘC chèn [X] vào ngay cuối câu đó. Ví dụ: 'Trái đất hình tròn [1][2].' KHÔNG liệt kê lại danh sách nguồn ở cuối câu trả lời.";
                if (restrictToDocs)
                {
                    promptSections.Add("Tài liệu liên quan:\n" + contextText + "\n\nChỉ sử dụng thông tin trong tài liệu trên để trả lời. Nếu tài liệu không đủ thông tin, hãy nói rõ ràng." + citationInstruction);
                }
                else
                {
                    promptSections.Add("Tài liệu liên quan (có thể tham khảo):\n" + contextText + "\n\nHãy ưu tiên sử dụng thông tin trong tài liệu này. Nếu tài liệu không đủ thông tin, bạn có thể sử dụng kiến thức sẵn có để trả lời." + citationInstruction);
                }
            }

            promptSections.Add($"Câu hỏi hiện tại: {message}");
            return string.Join("\n\n", promptSections);
        }

        /// <summary>
        /// Rerank chunks bằng AI (gemini-1.5-flash) — model riêng để tránh xung đột quota với chat model.
        /// Fallback về keyword scoring nếu AI bị lỗi hoặc 429.
        /// </summary>
        private async Task<List<DocumentChunk>> RerankChunksAsync(string query, List<DocumentChunk> chunks, int topN)
        {
            if (chunks.Count <= topN) return chunks;

            var promptSections = new List<string>
            {
                "Bạn là một hệ thống chấm điểm mức độ liên quan của tài liệu. Nhiệm vụ của bạn là chọn ra các đoạn tài liệu phù hợp nhất với câu hỏi.",
                $"Câu hỏi: {query}",
                "Danh sách các đoạn tài liệu:"
            };
            for (int i = 0; i < chunks.Count; i++)
                promptSections.Add($"[{i}] {chunks[i].Content}");
            promptSections.Add($@"Vui lòng trả về MẢNG JSON gồm tối đa {topN} chỉ số (index) của các đoạn tài liệu liên quan nhất đến câu hỏi, sắp xếp theo mức độ phù hợp giảm dần.
Ví dụ: [3, 0, 1, 5, 2]
CHỈ TRẢ VỀ MẢNG JSON, KHÔNG GIẢI THÍCH HOẶC THÊM BẤT KỲ VĂN BẢN NÀO KHÁC.");

            var prompt = string.Join("\n\n", promptSections);
            try
            {
                // Dùng gemini-2.0-flash riêng cho rerank → quota RPM độc lập với chat model
                // (gemini-1.5-flash cũ đã bị Google khai tử, luôn trả 404).
                // Truyền maxRetries = 1 (không retry, không fallback model) để nếu lỗi thì dùng keyword scoring ngay!
                var reply = await _geminiService.GenerateAnswerAsync(prompt, "gemini-2.0-flash", maxRetries: 1);

                // Bỏ qua nếu AI trả lỗi 429/503
                if (reply.StartsWith("⚠️") || reply.StartsWith("Lỗi khi gọi AI"))
                    return LocalKeywordRerank(query, chunks, topN);

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
                            if (idx >= 0 && idx < chunks.Count && !addedIndices.Contains(idx))
                            { reranked.Add(chunks[idx]); addedIndices.Add(idx); }
                        if (reranked.Count < topN)
                            reranked.AddRange(chunks.Where((c, i) => !addedIndices.Contains(i)).Take(topN - reranked.Count));
                        return reranked;
                    }
                }
            }
            catch { /* fallback xuống keyword scoring */ }

            return LocalKeywordRerank(query, chunks, topN);
        }

        /// <summary>
        /// Fallback rerank cục bộ bằng keyword scoring khi AI bị lỗi/429.
        /// </summary>
        private static List<DocumentChunk> LocalKeywordRerank(string query, List<DocumentChunk> chunks, int topN)
        {
            var queryTokens = System.Text.RegularExpressions.Regex
                .Replace(query.ToLowerInvariant(), @"[^\p{L}\p{N}\s]", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 1)
                .ToHashSet();

            if (queryTokens.Count == 0) return chunks.Take(topN).ToList();

            return chunks.Select(chunk =>
            {
                var content = System.Text.RegularExpressions.Regex
                    .Replace((chunk.Content ?? "").ToLowerInvariant(), @"[^\p{L}\p{N}\s]", " ");
                double score = queryTokens.Sum(token =>
                {
                    int count = 0, pos = 0;
                    while ((pos = content.IndexOf(token, pos, StringComparison.Ordinal)) >= 0)
                    { count++; pos += token.Length; }
                    return (double)count;
                });
                return (chunk, score);
            })
            .OrderByDescending(x => x.score)
            .Take(topN)
            .Select(x => x.chunk)
            .ToList();
        }
    }
}
