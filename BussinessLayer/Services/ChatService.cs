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
        private readonly ITokenUsageLogRepository _tokenUsageLogRepository;
        private readonly IMemoryCache _cache;

        public ChatService(
            IChatRepository chatRepository,
            IDocumentRepository documentRepository,
            IGeminiService geminiService,
            IUserRepository userRepository,
            ISubscriptionService subscriptionService,
            ITokenUsageLogRepository tokenUsageLogRepository,
            IMemoryCache cache)
        {
            _chatRepository = chatRepository;
            _documentRepository = documentRepository;
            _geminiService = geminiService;
            _userRepository = userRepository;
            _subscriptionService = subscriptionService;
            _tokenUsageLogRepository = tokenUsageLogRepository;
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

                // Luôn giới hạn trong tài liệu — đã bỏ chế độ "Tất cả" (AI trả lời tự do ngoài tài liệu).
                request.RestrictToDocs = true;

                var existingSession = await ResolveSessionAsync(userId, request.SessionId, request.Message, createIfMissing: false);
                var conversationHistory = BuildConversationHistory(existingSession?.Messages);

                var (quotaResult, user, effectivePlan, remainingShortTokens, remainingMonthTokens, isUsingExtraQuota)
                    = await CheckQuotaAsync(userId);

                if (!quotaResult.Success)
                    return quotaResult;

                // KHÔNG từ chối cứng khi thiếu môn học / thiếu ngữ cảnh nữa:
                // - Không chọn môn → semantic search trên TẤT CẢ các môn (BuildContextAsync).
                // - Không tìm thấy ngữ cảnh → vẫn gọi AI kèm lịch sử hội thoại, để các yêu cầu meta
                //   ("trả lời bằng tiếng Anh", "tóm tắt lại"...) được xử lý thay vì trả lời "không tìm thấy".
                var (contextText, citations) = await BuildContextAsync(request);

                var prompt = BuildPrompt(request.Message, conversationHistory, contextText, request.RestrictToDocs, effectivePlan, remainingShortTokens, remainingMonthTokens);

                string replyText;
                GeminiTokenUsage? usage = null;
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                    replyText = await _geminiService.GenerateAnswerAsync(prompt, request.ModelName, onUsage: u => usage = u);
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

                // Token thực tế từ usageMetadata; nếu API không trả về thì ước lượng (ký tự/4)
                var promptTokens = usage?.PromptTokens ?? EstimateTokenCount(prompt);
                var outputTokens = usage?.OutputTokens ?? EstimateTokenCount(replyText);

                await SaveMessagesAndUpdateSessionAsync(session, request.Message, replyText, citations, outputTokens);
                await UpdateQuotaAsync(user, isUsingExtraQuota, promptTokens + outputTokens);
                await LogTokenUsageAsync(userId, usage, promptTokens, outputTokens);

                return new ChatResponseDto
                {
                    Success = true,
                    Reply = replyText,
                    Remaining = ComputeRemainingTokens(user, effectivePlan, isUsingExtraQuota),
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

                // Luôn giới hạn trong tài liệu — đã bỏ chế độ "Tất cả" (AI trả lời tự do ngoài tài liệu).
                request.RestrictToDocs = true;

                var existingSession = await ResolveSessionAsync(userId, request.SessionId, request.Message, createIfMissing: false);
                var conversationHistory = BuildConversationHistory(existingSession?.Messages);

                var (quotaResult, user, effectivePlan, remainingShortTokens, remainingMonthTokens, isUsingExtraQuota)
                    = await CheckQuotaAsync(userId);

                if (!quotaResult.Success)
                    return quotaResult;

                // KHÔNG từ chối cứng khi thiếu môn học / thiếu ngữ cảnh nữa:
                // - Không chọn môn → semantic search trên TẤT CẢ các môn (BuildContextAsync).
                // - Không tìm thấy ngữ cảnh → vẫn gọi AI kèm lịch sử hội thoại, để các yêu cầu meta
                //   ("trả lời bằng tiếng Anh", "tóm tắt lại"...) được xử lý thay vì trả lời "không tìm thấy".
                var (contextText, citations) = await BuildContextAsync(request);

                var prompt = BuildPrompt(request.Message, conversationHistory, contextText, request.RestrictToDocs, effectivePlan, remainingShortTokens, remainingMonthTokens);

                // Stream từng chunk
                var fullReply = new StringBuilder();
                GeminiTokenUsage? usage = null;
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(90));

                await foreach (var chunk in _geminiService.GenerateStreamingAnswerAsync(prompt, request.ModelName, timeoutCts.Token, u => usage = u))
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

                // Token thực tế từ usageMetadata; nếu API không trả về thì ước lượng (ký tự/4)
                var promptTokens = usage?.PromptTokens ?? EstimateTokenCount(prompt);
                var outputTokens = usage?.OutputTokens ?? EstimateTokenCount(replyText);

                await SaveMessagesAndUpdateSessionAsync(session, request.Message, replyText, citations, outputTokens);
                await UpdateQuotaAsync(user, isUsingExtraQuota, promptTokens + outputTokens);
                await LogTokenUsageAsync(userId, usage, promptTokens, outputTokens);

                return new ChatResponseDto
                {
                    Success = true,
                    Reply = replyText,
                    Remaining = ComputeRemainingTokens(user, effectivePlan, isUsingExtraQuota),
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

        /// <summary>
        /// Kiểm tra quota token trước khi gọi AI. Chỉ chặn khi ĐÃ vượt hạn mức — số token của câu hỏi
        /// hiện tại chưa biết trước nên được trừ sau khi AI trả lời xong (câu cuối có thể vượt nhẹ hạn mức).
        /// </summary>
        private async Task<(ChatResponseDto quotaResult, User? user, string effectivePlan,
            long remainingShortTokens, long remainingMonthTokens, bool isUsingExtraQuota)>
            CheckQuotaAsync(int userId)
        {
            var effectivePlan = "Basic";
            var remainingShortTokens = long.MaxValue;
            var remainingMonthTokens = long.MaxValue;
            var isUsingExtraQuota = false;

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return (new ChatResponseDto { Success = true }, null, effectivePlan, remainingShortTokens, remainingMonthTokens, false);

            await _subscriptionService.CheckAndUpdateQuotaAsync(userId);
            user = await _userRepository.GetUserByIdAsync(userId) ?? user;

            var now = DateTime.UtcNow;
            var planActive = user.SubscriptionPlan == "Basic" || user.SubscriptionPlan == "Free" ||
                             (user.SubscriptionExpiry.HasValue && user.SubscriptionExpiry.Value >= now);
            effectivePlan = planActive ? user.SubscriptionPlan : "Basic";
            if (effectivePlan == "Free") effectivePlan = "Basic";

            var limitShort = TokenQuota.GetShortTermTokenLimit(effectivePlan);
            var limitMonth = TokenQuota.GetMonthlyTokenLimit(effectivePlan);

            remainingShortTokens = limitShort == long.MaxValue ? long.MaxValue : Math.Max(0, limitShort - user.ShortTermTokensUsed);
            remainingMonthTokens = limitMonth == long.MaxValue ? long.MaxValue : Math.Max(0, limitMonth - user.MonthlyTokensUsed);

            bool outOfStandardQuota = false;
            string outOfQuotaMessage = "";

            if (limitMonth != long.MaxValue && user.MonthlyTokensUsed >= limitMonth)
            {
                outOfStandardQuota = true;
                outOfQuotaMessage = $"Bạn đã dùng hết {limitMonth:N0} token trong tháng này. Vui lòng chờ sang chu kỳ sau hoặc nâng cấp gói.";
            }
            else if (limitShort != long.MaxValue && user.ShortTermTokensUsed >= limitShort)
            {
                outOfStandardQuota = true;
                var remainingTime = user.ShortTermResetDate.HasValue ? (user.ShortTermResetDate.Value - now) : TimeSpan.Zero;
                var hours = Math.Max(0, (int)Math.Ceiling(remainingTime.TotalHours));
                outOfQuotaMessage = effectivePlan == "Basic"
                    ? $"Bạn đã đạt giới hạn {limitShort:N0} token trong 5 giờ. Vui lòng chờ thêm {hours} giờ."
                    : $"Bạn đã đạt giới hạn {limitShort:N0} token trong 5 giờ của gói {effectivePlan}. Vui lòng chờ thêm {hours} giờ.";
            }

            if (outOfStandardQuota)
            {
                if (user.UseExtraQuota && user.ExtraTokenQuota > 0)
                {
                    isUsingExtraQuota = true;
                }
                else
                {
                    if (user.ExtraTokenQuota > 0)
                        outOfQuotaMessage += " Bạn vẫn còn token dự phòng, hãy BẬT công tắc 'Sử dụng token dự phòng' để tiếp tục.";
                    else
                        outOfQuotaMessage += " Bạn đã hết token. Vui lòng vào mục Gói Hội Viên để mua thêm token dự phòng.";

                    return (new ChatResponseDto { Success = false, OutOfQuota = true, Remaining = remainingShortTokens, Message = outOfQuotaMessage },
                        user, effectivePlan, remainingShortTokens, remainingMonthTokens, isUsingExtraQuota);
                }
            }

            return (new ChatResponseDto { Success = true }, user, effectivePlan, remainingShortTokens, remainingMonthTokens, isUsingExtraQuota);
        }

        /// <summary>Số token còn lại sau khi đã trừ quota (dùng cho response trả về client).</summary>
        private static long ComputeRemainingTokens(User? user, string effectivePlan, bool isUsingExtraQuota)
        {
            if (user == null) return long.MaxValue;
            if (isUsingExtraQuota) return user.ExtraTokenQuota;
            var limitShort = TokenQuota.GetShortTermTokenLimit(effectivePlan);
            return limitShort == long.MaxValue ? long.MaxValue : Math.Max(0, limitShort - user.ShortTermTokensUsed);
        }

        private async Task<(string contextText, List<CitationDto> citations)> BuildContextAsync(ChatRequestDto request)
        {
            var citations = new List<CitationDto>();
            var contextText = string.Empty;

            // SubjectId null/0 = tìm kiếm ngữ nghĩa trên tài liệu của TẤT CẢ các môn học
            int? subjectId = (request.SubjectId.HasValue && request.SubjectId.Value > 0) ? request.SubjectId.Value : null;

            // Cache key = subject ID (hoặc "all") + question hash
            var cacheKey = $"chunks_subject_{(subjectId?.ToString() ?? "all")}_{request.Message.GetHashCode()}";
            if (_cache.TryGetValue(cacheKey, out (string ctx, List<CitationDto> cit) cached))
                return (cached.ctx, cached.cit);

            List<DocumentChunk> similarChunks;
            try
            {
                var questionEmbedding = await _geminiService.GetEmbeddingAsync(request.Message);
                similarChunks = await _documentRepository.SearchSimilarChunksAsync(
                    new Vector(questionEmbedding), subjectId, topK: 20);
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

                var contextParts = new List<string>();
                foreach (var (chunk, index) in similarChunks.Select((c, i) => (c, i)))
                {
                    var (chapterTitle, sectionTitle) = DetectChapterAndSection(chunk.Document?.Content, chunk.Content);
                    chapterTitle ??= chunk.Document?.Chapter?.Title;

                    contextParts.Add(
                        $"Nguồn {index + 1}:\n" +
                        $"Tài liệu: {chunk.Document?.Title}\n" +
                        $"Môn: {chunk.Document?.Subject?.Name ?? "Không rõ"}\n" +
                        $"Chương: {chapterTitle ?? "Không rõ"}\n" +
                        $"Phần: {sectionTitle ?? "Không rõ"}\n" +
                        $"Đoạn: {chunk.OrderIndex}\n" +
                        $"Nội dung: {chunk.Content}");

                    citations.Add(new CitationDto
                    {
                        DocumentId = chunk.DocumentId,
                        DocumentTitle = chunk.Document?.Title ?? "Tài liệu",
                        SubjectName = chunk.Document?.Subject?.Name,
                        ChapterTitle = chapterTitle,
                        SectionTitle = sectionTitle,
                        ChunkOrderIndex = chunk.OrderIndex,
                        Snippet = BuildSnippet(chunk.Content),
                        FullContent = StripMarkdown(chunk.Content ?? "")
                    });
                }
                contextText = string.Join("\n\n", contextParts);
            }
            else if (subjectId.HasValue)
            {
                // Fallback (chỉ khi chọn 1 môn cụ thể): dùng thẳng nội dung tài liệu của môn đó
                var docs = await _documentRepository.GetDocumentsBySubjectIdAsync(subjectId.Value);
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
                        SubjectName = doc.Subject?.Name,
                        ChapterTitle = doc.Chapter?.Title,
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

        private static readonly System.Text.RegularExpressions.Regex ChapterHeadingRegex = new(
            @"(?:chương|chapter|chuong)\s+(?:[IVXLC]+|\d+)(?:\s*[:.\-–—]\s*[^.\n]{3,80})?",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        private static readonly System.Text.RegularExpressions.Regex SectionHeadingRegex = new(
            @"(?:phần|phan|part|mục|muc|section|bài|bai)\s+(?:[IVXLC]+|\d+)(?:\s*[:.\-–—]\s*[^.\n]{3,80})?|(?:^|\s)\d+\.\d+(?:\.\d+)?\s*[:.\-–—]?\s+(?-i:\p{Lu})[^.\n]{3,80}",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// Xác định chunk thuộc chương nào / phần nào của tài liệu:
        /// tìm vị trí chunk trong nội dung tài liệu rồi lấy heading (Chương X / Phần X / 1.2 ...) gần nhất phía trước.
        /// Nếu không định vị được, tìm heading ngay trong nội dung chunk.
        /// </summary>
        private static (string? chapterTitle, string? sectionTitle) DetectChapterAndSection(string? docContent, string? chunkContent)
        {
            if (string.IsNullOrWhiteSpace(chunkContent)) return (null, null);

            string? chapter = null, section = null;

            if (!string.IsNullOrWhiteSpace(docContent))
            {
                // Chunk được tạo bằng cách nối các từ với 1 dấu cách → chuẩn hóa whitespace để so khớp vị trí
                var normalizedDoc = System.Text.RegularExpressions.Regex.Replace(docContent, @"\s+", " ");
                var prefixLength = Math.Min(chunkContent.Length, 120);
                var prefix = chunkContent[..prefixLength];
                var pos = normalizedDoc.IndexOf(prefix, StringComparison.Ordinal);
                if (pos >= 0)
                {
                    var textBeforeChunk = normalizedDoc[..Math.Min(normalizedDoc.Length, pos + prefixLength)];
                    chapter = LastMatch(ChapterHeadingRegex, textBeforeChunk);
                    section = LastMatch(SectionHeadingRegex, textBeforeChunk);
                }
            }

            // Không định vị được trong tài liệu → tìm heading xuất hiện trong chính chunk
            chapter ??= FirstMatch(ChapterHeadingRegex, chunkContent);
            section ??= FirstMatch(SectionHeadingRegex, chunkContent);

            return (CleanHeading(chapter), CleanHeading(section));
        }

        private static string? LastMatch(System.Text.RegularExpressions.Regex regex, string text)
        {
            var matches = regex.Matches(text);
            return matches.Count > 0 ? matches[^1].Value : null;
        }

        private static string? FirstMatch(System.Text.RegularExpressions.Regex regex, string text)
        {
            var match = regex.Match(text);
            return match.Success ? match.Value : null;
        }

        private static string? CleanHeading(string? heading)
        {
            if (string.IsNullOrWhiteSpace(heading)) return null;
            var cleaned = System.Text.RegularExpressions.Regex.Replace(heading, @"\s+", " ").Trim().TrimEnd(':', '-', '–', '—', '.');
            if (cleaned.Length > 80) cleaned = cleaned[..80] + "...";
            return cleaned.Length == 0 ? null : cleaned;
        }

        private async Task SaveMessagesAndUpdateSessionAsync(
            ChatSession session, string userMessage, string replyText, List<CitationDto> citations, int? outputTokens = null)
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
                TokenCount = outputTokens ?? EstimateTokenCount(replyText),
                Timestamp = DateTime.UtcNow
            });

            if (title != session.Title)
                await _chatRepository.UpdateSessionTitleAsync(session.Id, title);
            else
                await _chatRepository.UpdateSessionUpdatedAtAsync(session.Id);
        }

        private async Task UpdateQuotaAsync(User? user, bool isUsingExtraQuota, long tokensUsed)
        {
            if (user == null) return;
            if (isUsingExtraQuota)
                user.ExtraTokenQuota = Math.Max(0, user.ExtraTokenQuota - tokensUsed);
            else
            {
                user.ShortTermTokensUsed += tokensUsed;
                user.MonthlyTokensUsed += tokensUsed;
            }
            await _userRepository.UpdateUserAsync(user);
        }

        /// <summary>Ghi nhật ký tiêu thụ token (phục vụ thống kê admin). Lỗi ghi log không làm hỏng câu chat.</summary>
        private async Task LogTokenUsageAsync(int userId, GeminiTokenUsage? usage, int promptTokens, int outputTokens)
        {
            try
            {
                await _tokenUsageLogRepository.AddAsync(new TokenUsageLog
                {
                    UserId = userId,
                    Feature = "chat",
                    Model = usage?.Model,
                    PromptTokens = promptTokens,
                    OutputTokens = outputTokens,
                    TotalTokens = promptTokens + outputTokens,
                    IsEstimated = usage == null,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch { /* không chặn luồng chat nếu ghi log lỗi */ }
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
            var recent = messages.OrderBy(m => m.Timestamp).TakeLast(maxMessages).ToList();

            var lines = new List<string>();
            for (int i = 0; i < recent.Count; i++)
            {
                var m = recent[i];
                var roleLabel = m.Role == "user" ? "Người dùng" : "Trợ lý AI";
                // 2 tin nhắn cuối (câu hỏi + câu trả lời gần nhất) giữ gần nguyên vẹn để các yêu cầu
                // "dịch lại / tóm tắt / viết lại câu trên" có đủ nội dung thao tác; tin cũ hơn cắt 500 ký tự.
                var maxLen = i >= recent.Count - 2 ? 6000 : 500;
                var text = m.Text.Length > maxLen ? m.Text[..maxLen] + "..." : m.Text;
                var marker = (m.Role != "user" && i == recent.Count - 1) || (m.Role != "user" && i == recent.Count - 2 && recent[^1].Role == "user")
                    ? " (CÂU TRẢ LỜI GẦN NHẤT CỦA BẠN)" : "";
                lines.Add($"{roleLabel}{marker}: {text}");
            }
            return lines.Count == 0 ? string.Empty : string.Join("\n", lines);
        }

        private static string BuildPrompt(
            string message,
            string conversationHistory,
            string contextText,
            bool restrictToDocs,
            string planName,
            long remainingShortTokens,
            long remainingMonthTokens)
        {
            var promptSections = new List<string>();

            if (remainingShortTokens != long.MaxValue || remainingMonthTokens != long.MaxValue)
            {
                var shortText = remainingShortTokens != long.MaxValue ? $"còn {remainingShortTokens:N0} token trong chu kỳ 5 giờ" : "không giới hạn trong 5 giờ";
                var monthText = remainingMonthTokens != long.MaxValue ? $"còn {remainingMonthTokens:N0} token trong tháng" : "không giới hạn trong tháng";
                promptSections.Add($"[THÔNG TIN HỆ THỐNG]\nNgười dùng đang sử dụng gói: {planName}. Hạn mức sử dụng tính theo token AI.\nTrước câu hỏi này: {shortText}, và {monthText}. (Câu hỏi này sẽ trừ thêm số token của chính nó).\n(Nếu người dùng hỏi về hạn mức còn lại, hãy dùng thông tin này để trả lời. Không cần nhắc đến nếu không liên quan).");
            }
            else
            {
                promptSections.Add($"[THÔNG TIN HỆ THỐNG]\nNgười dùng đang sử dụng gói: {planName} (Không giới hạn token).");
            }

            promptSections.Add("[QUAN TRỌNG VỀ DANH TÍNH CỦA BẠN]\nNếu người dùng hỏi bạn là ai, bạn được tạo ra bởi ai, hoặc bạn đang sử dụng mô hình/ngôn ngữ/AI nào (ví dụ: GPT, Gemini, Claude, LLaMA, v.v.), hãy TUYỆT ĐỐI KHÔNG tiết lộ mô hình thực sự của bạn. Thay vào đó, hãy trả lời một cách lịch sự rằng: 'Tôi là trợ lý AI được phát triển và tích hợp độc quyền bởi hệ thống ChatEdu để hỗ trợ bạn trong học tập.' (dịch câu này sang đúng ngôn ngữ mà người dùng đang dùng, ví dụ tiếng Anh: 'I am an AI assistant developed and exclusively integrated by the ChatEdu system to support your learning.')");

            if (!string.IsNullOrWhiteSpace(conversationHistory))
            {
                promptSections.Add("Lịch sử hội thoại gần đây:\n" + conversationHistory + "\n\nHãy giữ đúng ngữ cảnh hội thoại khi trả lời câu hỏi mới.");

                // Chỉ thị phân loại đặt NGAY SAU lịch sử (trước khối tài liệu) để không bị chôn vùi:
                // yêu cầu meta phải thao tác trên câu trả lời trước, không được trả lời như câu hỏi mới.
                promptSections.Add("[QUY TRÌNH BẮT BUỘC TRƯỚC KHI TRẢ LỜI]\nHãy xác định tin nhắn hiện tại thuộc loại nào:\n" +
                    "(a) CÂU HỎI KIẾN THỨC MỚI → trả lời dựa trên phần 'Tài liệu liên quan' (nếu có) theo các quy tắc bên dưới.\n" +
                    "(b) YÊU CẦU VỀ CÁCH TRÌNH BÀY / CHỈNH SỬA đối với nội dung đã trao đổi (ví dụ: 'hãy trả lời bằng tiếng Anh', 'dịch lại câu trên', 'tóm tắt ngắn gọn hơn', 'giải thích dễ hiểu hơn', 'trình bày dạng bảng') → BỎ QUA HOÀN TOÀN phần 'Tài liệu liên quan', lấy nguyên văn CÂU TRẢ LỜI GẦN NHẤT CỦA BẠN trong lịch sử hội thoại ở trên và biến đổi nó đúng theo yêu cầu (giữ nguyên nội dung, ý nghĩa và các trích dẫn [X] nếu có — KHÔNG viết câu trả lời mới, KHÔNG tự tìm lại trong tài liệu, KHÔNG trả lời 'tôi không tìm thấy thông tin trong tài liệu').");
            }

            // Nhắc lại ngoại lệ meta ở cuối khối tài liệu (phòng khi model chỉ đọc phần gần câu hỏi).
            var metaRequestInstruction = "\n\nNGOẠI LỆ QUAN TRỌNG: Nếu tin nhắn hiện tại là yêu cầu về cách trình bày cho nội dung đã trao đổi trước đó (loại (b) ở trên), hãy bỏ qua toàn bộ tài liệu này và biến đổi câu trả lời gần nhất của bạn theo yêu cầu.";

            if (!string.IsNullOrWhiteSpace(contextText))
            {
                var citationInstruction = "\n\nQUAN TRỌNG: Bắt buộc phải trích dẫn nguồn cho các câu văn có sử dụng thông tin từ tài liệu. Khi bạn viết một câu lấy thông tin từ 'Nguồn X', hãy BẮT BUỘC chèn [X] vào ngay cuối câu đó. Ví dụ: 'Trái đất hình tròn [1][2].' Nếu người dùng hỏi thông tin nằm ở đâu, hãy dựa vào thông tin Chương/Phần của nguồn để trả lời. KHÔNG liệt kê lại danh sách nguồn ở cuối câu trả lời.";
                if (restrictToDocs)
                {
                    promptSections.Add("Tài liệu liên quan:\n" + contextText
                        + "\n\nQUY TẮC BẮT BUỘC VỀ PHẠM VI KIẾN THỨC: Với câu hỏi kiến thức, CHỈ được sử dụng thông tin trong các tài liệu ở trên. TUYỆT ĐỐI KHÔNG bịa thêm, không dùng kiến thức bên ngoài tài liệu. Nếu tài liệu không chứa thông tin được hỏi, hãy trả lời rõ ràng rằng nội dung này không có trong tài liệu đã tải lên và gợi ý người dùng kiểm tra lại môn học/tài liệu."
                        + citationInstruction + metaRequestInstruction);
                }
                else
                {
                    promptSections.Add("Tài liệu liên quan (có thể tham khảo):\n" + contextText + "\n\nHãy ưu tiên sử dụng thông tin trong tài liệu này. Nếu tài liệu không đủ thông tin, bạn có thể sử dụng kiến thức sẵn có để trả lời, nhưng phải nói rõ phần nào là kiến thức ngoài tài liệu." + citationInstruction + metaRequestInstruction);
                }
            }
            else if (restrictToDocs)
            {
                // Không tìm thấy đoạn tài liệu liên quan nhưng vẫn để AI xử lý:
                // câu meta → làm theo lịch sử; câu kiến thức mới → nói rõ là không có trong tài liệu.
                promptSections.Add("LƯU Ý: Hệ thống KHÔNG tìm thấy đoạn tài liệu nào liên quan đến tin nhắn hiện tại."
                    + "\n- Nếu tin nhắn là yêu cầu về cách trình bày (dịch, tóm tắt, viết lại...) cho nội dung trước đó: thực hiện dựa trên lịch sử hội thoại."
                    + "\n- Nếu là câu hỏi kiến thức mới: trả lời rằng bạn không tìm thấy nội dung này trong các tài liệu đã tải lên, và gợi ý người dùng chọn đúng môn học hoặc chuyển sang chế độ 'Tất cả' để hỏi bằng kiến thức chung. KHÔNG được tự bịa câu trả lời.");
            }

            promptSections.Add(BuildLanguageInstruction(message));

            promptSections.Add($"Câu hỏi hiện tại: {message}");
            return string.Join("\n\n", promptSections);
        }

        // Từ tiếng Việt không dấu phổ biến — để không nhận nhầm "tom tat giup toi" là tiếng Anh.
        private static readonly HashSet<string> VietnameseAsciiWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "khong", "duoc", "nguoi", "trong", "nhung", "toi", "ban", "cua", "cho", "voi",
            "la", "va", "gi", "sao", "nao", "the", "nay", "do", "di", "lam", "hoc", "mon",
            "cau", "hoi", "tra", "loi", "giup", "minh", "tom", "tat", "giai", "thich",
            "tai", "lieu", "bai", "tap", "kiem", "diem", "noi", "dung", "phan", "chuong"
        };

        /// <summary>
        /// Phát hiện ngôn ngữ câu hỏi bằng heuristic (thay vì để model tự đoán — model hay bị
        /// cuốn theo prompt hệ thống tiếng Việt) rồi trả về chỉ thị tường minh, viết bằng chính
        /// ngôn ngữ đích để tăng trọng số.
        /// </summary>
        private static string BuildLanguageInstruction(string message)
        {
            // Có ký tự dấu tiếng Việt → chắc chắn tiếng Việt.
            const string vietnameseChars = "àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ";
            bool hasDiacritics = message.Any(c => vietnameseChars.Contains(char.ToLowerInvariant(c)));

            // Tiếng Việt gõ không dấu → vẫn nhận là tiếng Việt.
            bool hasViAsciiWord = !hasDiacritics && message
                .Split(new[] { ' ', ',', '.', '?', '!', ':', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(w => VietnameseAsciiWords.Contains(w));

            if (hasDiacritics || hasViAsciiWord)
                return "[QUY TẮC NGÔN NGỮ TRẢ LỜI]\nCâu hỏi hiện tại được viết bằng TIẾNG VIỆT. Hãy trả lời hoàn toàn bằng tiếng Việt. Ngoại lệ: nếu người dùng yêu cầu rõ ràng trả lời bằng ngôn ngữ khác thì làm theo yêu cầu đó; thuật ngữ chuyên ngành và trích dẫn nguyên văn từ tài liệu giữ nguyên ngôn ngữ gốc.";

            return "[MANDATORY RESPONSE LANGUAGE]\nThe user's current question is written in ENGLISH (or another non-Vietnamese language). You MUST write your ENTIRE answer in the same language as the question — including all explanations, notes, headings and citations context. Do NOT answer in Vietnamese just because these system instructions are written in Vietnamese. Exceptions: if the user explicitly asks for a specific language, follow that request; keep verbatim quotes from the documents in their original language.";
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
