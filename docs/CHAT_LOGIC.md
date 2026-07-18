# 💬 Logic Chat AI — Giải thích chi tiết & chỉ dẫn code

Tài liệu này giải thích **toàn bộ luồng xử lý một câu hỏi chat** — từ lúc người dùng gõ trên trình duyệt đến khi nhận câu trả lời kèm trích dẫn — và **chỉ ra chính xác đoạn code** chịu trách nhiệm cho từng bước, để bạn vừa đọc vừa mở source đối chiếu.

> Phần **hạn mức token** (đo, trừ, reset) được tách riêng ở [`TOKEN_LOGIC.md`](TOKEN_LOGIC.md). Tài liệu này tập trung vào *luồng chat và RAG*.

---

## 1. Kiến trúc 3 tầng

Chat AI chạy qua 3 tầng và có **2 đường truyền**:

- **Đường chính — Streaming** (chữ hiện dần) qua **SignalR** (realtime, WebSocket).
- **Đường dự phòng — Non-streaming** qua **AJAX**, tự động dùng khi trình duyệt không kết nối được SignalR.

```
┌─────────────── TRÌNH DUYỆT ───────────────┐
│  Pages/Chat/Index.cshtml (UI + JavaScript) │
│    │ SignalR (chính)      │ AJAX (dự phòng) │
└────┼─────────────────────┼──────────────────┘
     ▼                     ▼
SignalRHub            IndexModel.OnPostSendChatMessageAsync
.SendStreamingMessage       │
     │                     │
     └──────────┬───────────┘
                ▼
        ChatService                     ← TOÀN BỘ nghiệp vụ chat nằm ở đây
                ▼
        GeminiService  →  Google Gemini API
                ▼
        DocumentRepository (pgvector) + ChatRepository (Postgres)
```

**File chính:**

| File | Vai trò |
|---|---|
| [`PresentationLayer/Pages/Chat/Index.cshtml`](PresentationLayer/Pages/Chat/Index.cshtml) | Giao diện + JavaScript (kết nối SignalR, render tin nhắn, trích dẫn) |
| [`PresentationLayer/Pages/Chat/Index.cshtml.cs`](PresentationLayer/Pages/Chat/Index.cshtml.cs) | Các handler AJAX (danh sách phiên, quota, model, tin cũ, fallback gửi tin) |
| [`PresentationLayer/SignalR/SignalR.cs`](PresentationLayer/SignalR/SignalR.cs) | Hub SignalR — nhận `SendStreamingMessage`, trả `ReceiveChunk` / `StreamComplete` / `StreamError` |
| [`BussinessLayer/Services/ChatService.cs`](BussinessLayer/Services/ChatService.cs) | Nghiệp vụ chat: quota, RAG, dựng prompt, gọi AI, lưu tin nhắn |
| [`BussinessLayer/Services/GeminiService.cs`](BussinessLayer/Services/GeminiService.cs) | Gọi Gemini API (blocking + streaming), embedding, fallback model |
| [`DataAccessLayer/Repositories/DocumentRepository.cs`](DataAccessLayer/Repositories/DocumentRepository.cs) | `SearchSimilarChunksAsync` — tìm đoạn tài liệu tương đồng bằng pgvector |
| [`DataAccessLayer/Repositories/ChatRepository.cs`](DataAccessLayer/Repositories/ChatRepository.cs) | CRUD phiên chat & tin nhắn |

---

## 2. Tầng 1 — Nhận request từ trình duyệt

### Đường Streaming (mặc định) — SignalR

📄 [`SignalR.cs:29-87`](PresentationLayer/SignalR/SignalR.cs#L29-L87)

Client gọi method `SendStreamingMessage(request)` trên hub. Hub gọi `ChatService.ProcessStreamingChatMessageAsync` và truyền vào một **callback `onChunk`** — mỗi khi ChatService có một mẩu chữ mới, hub đẩy ngay về client:

```csharp
result = await _chatService.ProcessStreamingChatMessageAsync(
    userId,
    request,
    async chunk =>
    {
        // Gửi từng chunk về client → chữ hiện dần trên màn hình
        if (!cts.Token.IsCancellationRequested)
            await Clients.Caller.SendAsync("ReceiveChunk", chunk, cts.Token);
    },
    cts.Token);
```

Sau khi xong:
- Lỗi → gửi `StreamError` (kèm cờ `OutOfQuota` nếu hết token) — [`SignalR.cs:72-76`](PresentationLayer/SignalR/SignalR.cs#L72-L76)
- Thành công → gửi `StreamComplete` với `sessionId`, `sessionTitle`, `remaining` (token còn lại), `citations`, `reply` — [`SignalR.cs:78-86`](PresentationLayer/SignalR/SignalR.cs#L78-L86)

Hub cũng **huỷ stream khi client ngắt kết nối** để không tốn API vô ích — [`SignalR.cs:46`](PresentationLayer/SignalR/SignalR.cs#L46):
```csharp
Context.ConnectionAborted.Register(() => cts.Cancel());
```

### Đường dự phòng — AJAX non-streaming

📄 [`Index.cshtml.cs:125-145`](PresentationLayer/Pages/Chat/Index.cshtml.cs#L125-L145)

Khi trình duyệt không dùng được SignalR, JS gọi handler `?handler=SendChatMessage`, chạy `ProcessChatMessageAsync` (trả về nguyên khối, không streaming).

> **Cả 2 đường đổ về cùng `ChatService`** với logic gần như giống hệt — khác biệt duy nhất là streaming dùng `yield`/callback để trả từng chữ, còn blocking `await` một lần rồi trả cả câu.

Các handler AJAX phụ trợ khác trong `Index.cshtml.cs`: danh sách phiên (`OnGetSessions`), quota realtime (`OnGetQuotaInfo`), danh sách model (`OnGetModels`), tin nhắn cũ phân trang (`OnGetSessionMessages`), tạo/xoá/xoá-nội-dung phiên.

---

## 3. Tầng 2 — ChatService: 6 bước xử lý một câu hỏi

Đây là trái tim của tính năng. Hàm streaming: 📄 [`ChatService.cs:174-256`](BussinessLayer/Services/ChatService.cs#L174-L256) (bản blocking `ProcessChatMessageAsync` ở [`dòng 96-171`](BussinessLayer/Services/ChatService.cs#L96-L171) song song từng bước).

### Bước 0 — Validate độ dài

📄 [`ChatService.cs:182-187`](BussinessLayer/Services/ChatService.cs#L182-L187) — chặn tin nhắn quá 8.000 ký tự (`MaxMessageLength`).

### Bước 1 — Lấy phiên chat + dựng lịch sử hội thoại

📄 [`ChatService.cs:189-190`](BussinessLayer/Services/ChatService.cs#L189-L190)
```csharp
var existingSession = await ResolveSessionAsync(userId, request.SessionId, request.Message, createIfMissing: false);
var conversationHistory = BuildConversationHistory(existingSession?.Messages);
```

`BuildConversationHistory` — 📄 [`ChatService.cs:649-668`](BussinessLayer/Services/ChatService.cs#L649-L668) — đây là cách AI **"nhớ" hội thoại**:
```csharp
var recent = messages.OrderBy(m => m.Timestamp).TakeLast(maxMessages).ToList();  // 8 tin gần nhất
...
// 2 tin nhắn cuối (câu hỏi + câu trả lời gần nhất) giữ tới 6.000 ký tự để làm được
// "dịch lại câu trên / tóm tắt lại"; tin cũ hơn cắt 500 ký tự cho gọn.
var maxLen = i >= recent.Count - 2 ? 6000 : 500;
...
var marker = ... ? " (CÂU TRẢ LỜI GẦN NHẤT CỦA BẠN)" : "";  // đánh dấu để AI biết thao tác trên đâu
```

> Đây chính là phần đã sửa cho lỗi *"yêu cầu trình bày lại nhưng AI làm thành câu mới"*: trước đây mọi tin nhắn đều bị cắt 500 ký tự nên AI không đủ nội dung câu trả lời trước để biến đổi.

### Bước 2 — Kiểm tra quota token

📄 [`ChatService.cs:192-196`](BussinessLayer/Services/ChatService.cs#L192-L196) → `CheckQuotaAsync` ([`dòng 264-329`](BussinessLayer/Services/ChatService.cs#L264-L329)). Hết token thì `return` luôn, **không gọi AI**. Chi tiết logic token xem [`TOKEN_LOGIC.md`](TOKEN_LOGIC.md).

### Bước 3 — Dựng ngữ cảnh RAG (tìm tài liệu liên quan)

📄 [`ChatService.cs:202`](BussinessLayer/Services/ChatService.cs#L202) → `BuildContextAsync` ([`dòng 340-426`](BussinessLayer/Services/ChatService.cs#L340-L426)). Đây là phần **RAG (Retrieval-Augmented Generation)** — tìm đoạn tài liệu đúng chủ đề rồi đưa cho AI:

**3.1 — Xác định phạm vi môn học** ([`dòng 346`](BussinessLayer/Services/ChatService.cs#L346)):
```csharp
// SubjectId null/0 = tìm trên tài liệu của TẤT CẢ các môn; có giá trị = lọc theo môn đó
int? subjectId = (request.SubjectId.HasValue && request.SubjectId.Value > 0) ? request.SubjectId.Value : null;
```

**3.2 — Cache 5 phút** ([`dòng 348-351`](BussinessLayer/Services/ChatService.cs#L348-L351)): key = môn + hash câu hỏi → câu hỏi lặp lại không tốn embedding lại.

**3.3 — Embedding + Semantic Search** ([`dòng 354-365`](BussinessLayer/Services/ChatService.cs#L354-L365)):
```csharp
var questionEmbedding = await _geminiService.GetEmbeddingAsync(request.Message);      // vector hoá câu hỏi
similarChunks = await _documentRepository.SearchSimilarChunksAsync(                    // tìm bằng pgvector
    new Vector(questionEmbedding), subjectId, topK: 20);
```
`SearchSimilarChunksAsync` — 📄 [`DocumentRepository.cs:110-127`](DataAccessLayer/Repositories/DocumentRepository.cs#L110-L127) — sắp xếp theo **CosineDistance** (khoảng cách vector) để lấy 20 đoạn gần nghĩa nhất.

**3.4 — Rerank chọn top 5** ([`dòng 369`](BussinessLayer/Services/ChatService.cs#L369)) → `RerankChunksAsync` ([`dòng 739-789`](BussinessLayer/Services/ChatService.cs#L739-L789)): nhờ AI chấm điểm mức độ liên quan và chọn 5 đoạn tốt nhất. Nếu AI lỗi/429 → rơi về `LocalKeywordRerank` (chấm điểm theo từ khoá, chạy cục bộ, miễn phí — [`dòng 794-821`](BussinessLayer/Services/ChatService.cs#L794-L821)).

**3.5 — Ghép ngữ cảnh + tạo trích dẫn** ([`dòng 372-398`](BussinessLayer/Services/ChatService.cs#L372-L398)): mỗi đoạn thành một "Nguồn X" (kèm tài liệu/môn/chương/phần) và một `CitationDto` (để hiển thị badge `[1][2]` và deep-link về tài liệu gốc).

> **Vì sao đây KHÔNG phải "so chuỗi"?** Hệ thống dùng **vector embedding + semantic search + AI rerank** — hiểu *ngữ nghĩa* câu hỏi chứ không so ký tự. Việc so khớp từ khoá (`LocalKeywordRerank`) chỉ là phương án dự phòng khi API embedding/rerank bị lỗi.

### Bước 4 — Dựng prompt gửi cho AI

📄 [`ChatService.cs:204`](BussinessLayer/Services/ChatService.cs#L204) → `BuildPrompt` ([`dòng 670-733`](BussinessLayer/Services/ChatService.cs#L670-L733)). Prompt ghép theo thứ tự:

1. **`[THÔNG TIN HỆ THỐNG]`** — gói + hạn mức token còn lại ([`dòng 681-690`](BussinessLayer/Services/ChatService.cs#L681-L690))
2. **`[QUAN TRỌNG VỀ DANH TÍNH]`** — chỉ thị AI **không được lộ mình là Gemini**, phải xưng là "trợ lý AI của ChatEdu" ([`dòng 692`](BussinessLayer/Services/ChatService.cs#L692))
3. **Lịch sử hội thoại** ([`dòng 694-696`](BussinessLayer/Services/ChatService.cs#L694-L696))
4. **`[QUY TRÌNH BẮT BUỘC]`** — phân loại tin nhắn: (a) câu hỏi kiến thức mới → dùng tài liệu; (b) yêu cầu trình bày lại (dịch/tóm tắt/viết lại) → **biến đổi câu trả lời gần nhất, không tìm lại trong tài liệu** ([`dòng 700-702`](BussinessLayer/Services/ChatService.cs#L700-L702))
5. **Tài liệu liên quan** — kèm quy tắc bắt buộc trích dẫn `[X]` và **cấm dùng kiến thức ngoài tài liệu** (hệ thống luôn chạy chế độ giới hạn trong tài liệu — xem mục 5) ([`dòng 708-729`](BussinessLayer/Services/ChatService.cs#L708-L729))
6. **Câu hỏi hiện tại** ([`dòng 731`](BussinessLayer/Services/ChatService.cs#L731))

### Bước 5 — Gọi Gemini streaming

📄 [`ChatService.cs:206-217`](BussinessLayer/Services/ChatService.cs#L206-L217)
```csharp
var fullReply = new StringBuilder();
GeminiTokenUsage? usage = null;
using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
timeoutCts.CancelAfter(TimeSpan.FromSeconds(90));   // timeout tổng 90 giây

await foreach (var chunk in _geminiService.GenerateStreamingAnswerAsync(prompt, request.ModelName, timeoutCts.Token, u => usage = u))
{
    if (cancellationToken.IsCancellationRequested) break;
    fullReply.Append(chunk);   // gom lại để lưu vào DB
    await onChunk(chunk);      // đẩy về client hiện dần
}
```

### Bước 6 — Hoàn tất

📄 [`ChatService.cs:219-246`](BussinessLayer/Services/ChatService.cs#L219-L246)
- Nếu reply rỗng hoặc bắt đầu bằng `⚠️`/`Lỗi khi gọi AI` → coi là lỗi, **không lưu tin nhắn, không trừ token** ([`dòng 220-224`](BussinessLayer/Services/ChatService.cs#L220-L224))
- Thành công → lưu 2 tin nhắn (user + model, citations dạng JSON), trừ token, ghi log, trả về `remaining` ([`dòng 234-246`](BussinessLayer/Services/ChatService.cs#L234-L246))

`SaveMessagesAndUpdateSessionAsync` ([`dòng 489-521`](BussinessLayer/Services/ChatService.cs#L489-L521)): lưu tin nhắn và **đặt tiêu đề phiên** = 22 ký tự đầu câu hỏi đầu tiên.

---

## 4. Tầng 3 — GeminiService: gọi API + fallback model

### Streaming (SSE)

📄 [`GeminiService.cs:228-313`](BussinessLayer/Services/GeminiService.cs#L228-L313)

**Chuỗi model fallback** — điểm quan trọng nhất ([`dòng 240-263`](BussinessLayer/Services/GeminiService.cs#L240-L263)): thử model chính trước, gặp **429/503/500/404** thì chuyển ngay sang model dự phòng kế tiếp:
```
model người dùng chọn → gemini-flash-lite-latest → gemini-3.5-flash → gemini-2.0-flash
```
Lý do: quota Gemini tính **riêng theo từng model**, nên đổi model là có ngay quota mới thay vì ngồi chờ. Danh sách ở [`GeminiService.cs:28-33`](BussinessLayer/Services/GeminiService.cs#L28-L33).

**Đọc SSE** ([`dòng 281-310`](BussinessLayer/Services/GeminiService.cs#L281-L310)): đọc từng dòng `data: {...}`, tách phần `text` và `yield return` từng mẩu cho ChatService.

### Embedding

📄 [`GeminiService.cs:139-157`](BussinessLayer/Services/GeminiService.cs#L139-L157) — dùng model `gemini-embedding-001`, cắt còn 768 chiều để khớp cột vector trong DB.

### Retry / backoff (đường blocking)

📄 [`GeminiService.cs:103-136`](BussinessLayer/Services/GeminiService.cs#L103-L136) — `PostWithRetryAsync`: retry tối đa 4 lần với backoff 5s/15s/45s cho lỗi 429/503/500, tôn trọng header `Retry-After` nhưng cap 30 giây.

---

## 5. Tầng UI — JavaScript trong Index.cshtml

Các điểm chính trong [`Pages/Chat/Index.cshtml`](PresentationLayer/Pages/Chat/Index.cshtml):

| Chức năng | Mô tả |
|---|---|
| Kết nối SignalR | Lắng nghe `ReceiveChunk` (chữ hiện dần), `StreamComplete` (chốt tin), `StreamError` (báo lỗi / mở modal hết token) |
| `selectSubject(id)` | Chọn phạm vi môn học / "Tất cả môn học" cho RAG |
| Phạm vi kiến thức | **Luôn cố định "Chỉ trả lời trong tài liệu"** — đã bỏ chế độ "Tất cả" (AI trả lời tự do). `restrictToDocs` luôn `true` và còn được ép lại ở backend |
| `renderMdCit()` | Render Markdown + biến `[1][2]` thành **badge trích dẫn** click được |
| `openCit()` | Mở modal trích dẫn, có nút **"Mở tài liệu tại vị trí này"** (deep-link `/ViewDocument?id=X&chunk=N`) |
| `refreshQuota()` | Cập nhật thanh token còn lại sau mỗi câu |

---

## 6. Tóm tắt — tra nhanh

| Câu hỏi | Trả lời ngắn | Code |
|---|---|---|
| Chữ hiện dần nhờ đâu? | `IAsyncEnumerable` + `yield return` từng chunk SSE → callback `onChunk` → SignalR `ReceiveChunk` | [`ChatService.cs:212-217`](BussinessLayer/Services/ChatService.cs#L212-L217), [`SignalR.cs:54-59`](PresentationLayer/SignalR/SignalR.cs#L54-L59) |
| AI "nhớ" hội thoại nhờ đâu? | Gửi kèm 8 tin nhắn gần nhất vào prompt | [`ChatService.cs:649-668`](BussinessLayer/Services/ChatService.cs#L649-L668) |
| "Tìm đúng tài liệu" hoạt động sao? | Embedding → pgvector semantic search → AI rerank top 5 | [`ChatService.cs:340-426`](BussinessLayer/Services/ChatService.cs#L340-L426) |
| Chọn "tất cả môn" xử lý sao? | `SubjectId` null → search không lọc môn (vẫn chỉ trong tài liệu) | [`ChatService.cs:346`](BussinessLayer/Services/ChatService.cs#L346) |
| AI có trả lời ngoài tài liệu không? | **Không** — luôn giới hạn trong tài liệu, `RestrictToDocs` bị ép `true` ở backend | [`ChatService.cs:111`](BussinessLayer/Services/ChatService.cs#L111), [`:193`](BussinessLayer/Services/ChatService.cs#L193) |
| Yêu cầu "dịch lại câu trên" xử lý sao? | Prompt phân loại (a)/(b) + giữ nguyên câu trả lời gần nhất trong lịch sử | [`ChatService.cs:700-702`](BussinessLayer/Services/ChatService.cs#L700-L702) |
| Vì sao AI không lộ là Gemini? | Chỉ thị ẩn danh trong prompt | [`ChatService.cs:692`](BussinessLayer/Services/ChatService.cs#L692) |
| Hết quota API 1 model thì sao? | Tự động fallback sang model khác | [`GeminiService.cs:240-263`](BussinessLayer/Services/GeminiService.cs#L240-L263) |
| Không kết nối được SignalR? | Tự động fallback sang AJAX non-streaming | [`Index.cshtml.cs:125-145`](PresentationLayer/Pages/Chat/Index.cshtml.cs#L125-L145) |

---

## 7. Sơ đồ luồng đầy đủ

```
Người dùng gõ câu hỏi + chọn phạm vi môn (một môn cụ thể hoặc "Tất cả môn học")
        (phạm vi kiến thức luôn cố định: chỉ trả lời trong tài liệu)
        │
        ▼  SignalR.SendStreamingMessage (hoặc AJAX fallback)
ChatService.ProcessStreamingChatMessageAsync
        │
        ├─ 1. ResolveSession + BuildConversationHistory   (lấy phiên + 8 tin gần nhất)
        ├─ 2. CheckQuotaAsync                              (còn token? hết → chặn)
        ├─ 3. BuildContextAsync  ── Embedding ── pgvector search ── Rerank top 5  (RAG)
        ├─ 4. BuildPrompt        (system + danh tính + lịch sử + phân loại + tài liệu + câu hỏi)
        ├─ 5. GeminiService.GenerateStreamingAnswerAsync   (gọi API, fallback model, SSE)
        │        └─► mỗi chunk ──► onChunk ──► ReceiveChunk ──► chữ hiện dần trên UI
        └─ 6. Lưu 2 tin nhắn + trừ token + ghi log + StreamComplete (citations, remaining)
```
