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
    public class ChatService : IChatService
    {
        private const string DefaultSessionTitle = "Cuoc tro chuyen moi";
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
                        return new ChatResponseDto { Success = false, Message = "Hay chon it nhat mot tai lieu truoc khi hoi trong che do han che theo tai lieu." };
                    if (string.IsNullOrWhiteSpace(contextText))
                        return new ChatResponseDto { Success = false, Message = "Toi khong tim thay doan tai lieu phu hop de tra loi cau hoi nay trong cac tai lieu da chon." };
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
                    return new ChatResponseDto { Success = false, Message = "Yeu cau AI vuot qua 60 giay, vui long thu lai sau." };
                }

                if (string.IsNullOrWhiteSpace(replyText))
                    return new ChatResponseDto { Success = false, Message = "AI khong tra ve noi dung hop le." };

                var session = existingSession ?? await ResolveSessionAsync(userId, request.SessionId, request.Message, createIfMissing: true);
                if (session == null)
                    return new ChatResponseDto { Success = false, Message = "Khong the tao hoac tim thay phien chat." };

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
                return new ChatResponseDto { Success = false, Message = "Loi he thong: " + ex.Message };
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
                        Message = $"Tin nhan qua dai ({request.Message.Length} ky tu). Gioi han la {MaxMessageLength} ky tu."
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
                        return new ChatResponseDto { Success = false, Message = "Hay chon it nhat mot tai lieu truoc khi hoi trong che do han che theo tai lieu." };
                    if (string.IsNullOrWhiteSpace(contextText))
                        return new ChatResponseDto { Success = false, Message = "Toi khong tim thay doan tai lieu phu hop de tra loi cau hoi nay." };
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
                    return new ChatResponseDto { Success = false, Message = "AI khong tra ve noi dung hop le." };

                var session = existingSession ?? await ResolveSessionAsync(userId, request.SessionId, request.Message, createIfMissing: true);
                if (session == null)
                    return new ChatResponseDto { Success = false, Message = "Khong the tao hoac tim thay phien chat." };

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
                return new ChatResponseDto { Success = false, Message = "Yeu cau AI het thoi gian cho (90 giay), vui long thu lai." };
            }
            catch (Exception ex)
            {
                return new ChatResponseDto { Success = false, Message = "Loi he thong: " + ex.Message };
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
                outOfQuotaMessage = $"Ban da dung het {limitMonth} cau hoi trong thang nay. Vui long cho sang thang sau hoac nang cap goi.";
            }
            else if (limitShort != int.MaxValue && user.ShortTermQuestionCount >= limitShort)
            {
                outOfStandardQuota = true;
                var remainingTime = user.ShortTermResetDate.HasValue ? (user.ShortTermResetDate.Value - now) : TimeSpan.Zero;
                var hours = Math.Max(0, (int)Math.Ceiling(remainingTime.TotalHours));
                outOfQuotaMessage = effectivePlan == "Basic"
                    ? $"Ban da dat gioi han {limitShort} cau hoi trong 5 gio. Vui long cho them {hours} gio."
                    : $"Ban da dat gioi han {limitShort} cau hoi trong 5 gio cua goi {effectivePlan}. Vui long cho them {hours} gio.";
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
                        outOfQuotaMessage += " Ban van con luot hoi du phong, hay BAT cong tac 'Su dung luot du phong' de tiep tuc.";
                    else
                        outOfQuotaMessage += " Ban da het luot hoi. Vui long vao muc Goi Hoi Vien de mua them luot du phong.";

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

            var questionEmbedding = await _geminiService.GetEmbeddingAsync(request.Message);
            var similarChunks = await _documentRepository.SearchSimilarChunksAsync(
                new Vector(questionEmbedding), request.SelectedDocIds, topK: 20);

            if (similarChunks.Any())
            {
                similarChunks = await RerankChunksAsync(request.Message, similarChunks, topN: 5);
                contextText = string.Join("\n\n", similarChunks.Select((chunk, index) =>
                    $"Nguon {index + 1}:\n" +
                    $"Tai lieu: {chunk.Document.Title}\n" +
                    $"Mon: {chunk.Document.Subject?.Name ?? "Khong ro"}\n" +
                    $"Chuong: {chunk.Document.Chapter?.Title ?? "Khong ro"}\n" +
                    $"Doan: {chunk.OrderIndex}\n" +
                    $"Noi dung: {chunk.Content}"));

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
                    var fullContent = fullCleaned.Length > 3000 ? fullCleaned[..3000] + "...\n(Tai lieu qua dai)" : fullCleaned;
                    contextText += $"Tai lieu: {doc.Title}\nNoi dung: {snippet}\n\n";
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
                    var roleLabel = m.Role == "user" ? "Nguoi dung" : "Tro ly AI";
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
                var shortText = remainingShortQueries != int.MaxValue ? $"con {remainingShortQueries} luot trong chu ky 5 gio" : "khong gioi han trong 5 gio";
                var monthText = remainingMonthQueries != int.MaxValue ? $"con {remainingMonthQueries} luot trong thang" : "khong gioi han trong thang";
                promptSections.Add($"[THONG TIN HE THONG]\nNguoi dung dang su dung goi: {planName}.\nSo luot hoi con lai sau cau hoi nay: {shortText}, va {monthText}.\n(Neu nguoi dung hoi ve so luot con lai, hay dung thong tin nay de tra loi. Khong can nhac den neu khong lien quan).");
            }
            else
            {
                promptSections.Add($"[THONG TIN HE THONG]\nNguoi dung dang su dung goi: {planName} (Khong gioi han so luot hoi).");
            }

            promptSections.Add("[QUAN TRONG VE DANH TINH CUA BAN]\nNeu nguoi dung hoi ban la ai, ban duoc tao ra boi ai, hoac ban dang su dung mo hinh/ngon ngu/AI nao (vi du: GPT, Gemini, Claude, LLaMA, v.v.), hay TUYET DOI KHONG tiet lo mo hinh thuc su cua ban. Thay vao do, hay tra loi mot cach lich su rang: 'Toi la tro ly AI duoc phat trien va tich hop doc quyen boi he thong ChatEdu de ho tro ban trong hoc tap.'");

            if (!string.IsNullOrWhiteSpace(conversationHistory))
            {
                promptSections.Add("Lich su hoi thoai gan day:\n" + conversationHistory + "\n\nHay giu dung ngu canh hoi thoai khi tra loi cau hoi moi.");
            }

            if (!string.IsNullOrWhiteSpace(contextText))
            {
                var citationInstruction = "\n\nQUAN TRONG: Bat buoc phai trich dan nguon cho cac cau van co su dung thong tin tu tai lieu. Khi ban viet mot cau lay thong tin tu 'Nguon X', hay BAT BUOC chen [X] vao ngay cuoi cau do. Vi du: 'Trai dat hinh tron [1][2].' KHONG liet ke lai danh sach nguon o cuoi cau tra loi.";
                if (restrictToDocs)
                {
                    promptSections.Add("Tai lieu lien quan:\n" + contextText + "\n\nChi su dung thong tin trong tai lieu tren de tra loi. Neu tai lieu khong du thong tin, hay noi ro rang." + citationInstruction);
                }
                else
                {
                    promptSections.Add("Tai lieu lien quan (co the tham khao):\n" + contextText + "\n\nHay uu tien su dung thong tin trong tai lieu nay. Neu tai lieu khong du thong tin, ban co the su dung kien thuc san co de tra loi." + citationInstruction);
                }
            }

            promptSections.Add($"Cau hoi hien tai: {message}");
            return string.Join("\n\n", promptSections);
        }

        private async Task<List<DocumentChunk>> RerankChunksAsync(string query, List<DocumentChunk> chunks, int topN)
        {
            if (chunks.Count <= topN) return chunks;

            var promptSections = new List<string>
            {
                "Ban la mot he thong cham diem muc do lien quan cua tai lieu. Nhiem vu cua ban la chon ra cac doan tai lieu phu hop nhat voi cau hoi.",
                $"Cau hoi: {query}",
                "Danh sach cac doan tai lieu:"
            };
            for (int i = 0; i < chunks.Count; i++)
                promptSections.Add($"[{i}] {chunks[i].Content}");
            promptSections.Add($@"Vui long tra ve MANG JSON gom toi da {topN} chi so (index) cua cac doan tai lieu lien quan nhat den cau hoi, sap xep theo muc do phu hop giam dan.
Vi du: [3, 0, 1, 5, 2]
CHI TRA VE MANG JSON, KHONG GIAI THICH HOAC THEM BAT KY VAN BAN NAO KHAC.");

            var prompt = string.Join("\n\n", promptSections);
            try
            {
                var reply = await _geminiService.GenerateAnswerAsync(prompt, "gemini-1.5-flash");
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
            catch { /* fallback */ }

            return chunks.Take(topN).ToList();
        }
    }
}
