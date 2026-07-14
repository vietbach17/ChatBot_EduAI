# 📖 Tài liệu tính năng Chat AI (ChatEdu)

Tài liệu này mô tả **toàn bộ luồng hoạt động** của tính năng Chat AI — từ lúc người dùng gõ câu hỏi trên trình duyệt cho đến khi nhận câu trả lời kèm trích dẫn — và **hướng dẫn chẩn đoán, sửa lỗi** khi có sự cố (đặc biệt là lỗi 429 "Trợ lý AI đang quá tải").

---

## 1. Tổng quan kiến trúc

```
┌─────────────────────  Trình duyệt  ─────────────────────┐
│  Pages/Chat/Index.cshtml (UI + JS)                       │
│    │ SignalR (streaming)          │ AJAX (fallback)      │
└────┼──────────────────────────────┼──────────────────────┘
     ▼                              ▼
┌─ PresentationLayer ──────────────────────────────────────┐
│  SignalR/SignalR.cs (SignalRHub, endpoint /courseHub)    │
│  Pages/Chat/Index.cshtml.cs (các handler AJAX)           │
└────┼─────────────────────────────────────────────────────┘
     ▼
┌─ BussinessLayer ─────────────────────────────────────────┐
│  Services/ChatService.cs   ← điều phối toàn bộ luồng chat│
│  Services/GeminiService.cs ← gọi Google Gemini API       │
└────┼──────────────────┬──────────────────────────────────┘
     ▼                  ▼
┌─ DataAccessLayer ─┐  ┌─ Google Gemini API ──────────────┐
│  ChatRepository   │  │  generateContent (trả lời)       │
│  DocumentRepo     │  │  streamGenerateContent (SSE)     │
│  (PostgreSQL +    │  │  embedContent (vector hoá)       │
│   pgvector)       │  └──────────────────────────────────┘
└───────────────────┘
```

### Các file quan trọng

| File | Vai trò |
|---|---|
| `PresentationLayer/Pages/Chat/Index.cshtml(.cs)` | Trang chat: UI, các handler AJAX (sessions, quota, models, tin nhắn cũ, fallback non-streaming) |
| `PresentationLayer/SignalR/SignalR.cs` | Hub SignalR `/courseHub` — nhận `SendStreamingMessage`, trả về `ReceiveChunk` / `StreamComplete` / `StreamError` |
| `BussinessLayer/Services/ChatService.cs` | Nghiệp vụ chat: kiểm tra quota, RAG (semantic search + rerank), dựng prompt, lưu tin nhắn |
| `BussinessLayer/Services/GeminiService.cs` | Gọi Gemini API: trả lời (blocking + streaming), embedding, retry + **fallback model** |
| `DataAccessLayer/Repositories/ChatRepository.cs` | CRUD phiên chat & tin nhắn |
| `DataAccessLayer/Repositories/DocumentRepository.cs` | `SearchSimilarChunksAsync` — tìm đoạn tài liệu tương đồng bằng pgvector |
| `.env` (gốc dự án) | `GEMINI_API_KEY`, `GEMINI_MODEL` — nạp lúc khởi động qua `Env.Load("../.env")` trong `Program.cs` |

---

## 2. Luồng xử lý một câu hỏi (từ đầu tới cuối)

### Bước 0 — Người dùng mở trang Chat
- `OnGetAsync` nạp danh sách tài liệu + thông tin gói hội viên.
- JS gọi thêm: `?handler=Sessions` (danh sách phiên), `?handler=Models` (danh sách model Gemini), `?handler=QuotaInfo` (quota còn lại).
- Trình duyệt kết nối SignalR tới `/courseHub`.

### Bước 1 — Gửi tin nhắn
Client gọi hub method `SendStreamingMessage(ChatRequestDto)` với:
- `Message` — câu hỏi (tối đa **8000 ký tự**)
- `SessionId` — phiên hiện tại (null → tạo phiên mới sau khi AI trả lời thành công)
- `SelectedDocIds` — danh sách tài liệu được tick chọn (để hỏi theo tài liệu)
- `RestrictToDocs` — `true` = chỉ trả lời trong phạm vi tài liệu
- `ModelName` — model người dùng chọn (trống → dùng `GEMINI_MODEL` trong `.env`)

Nếu trình duyệt không hỗ trợ SignalR, JS fallback sang AJAX `?handler=SendChatMessage` (non-streaming, dùng `ProcessChatMessageAsync`).

### Bước 2 — Kiểm tra quota (`ChatService.CheckQuotaAsync`)
Giới hạn theo gói (định nghĩa trong `ChatService`):

| Gói | Giới hạn 5 giờ | Giới hạn tháng |
|---|---|---|
| Basic/Free | 10 câu | 50 câu |
| Pro | 20 câu | 500 câu |
| Ultra | Không giới hạn | Không giới hạn |

- Hết quota chuẩn nhưng còn **lượt dự phòng** (`ExtraQuestionQuota`) và đã bật công tắc → vẫn cho hỏi, trừ vào lượt dự phòng.
- Hết sạch → trả về `OutOfQuota = true` kèm thông báo, **không gọi AI**.
- Quota chỉ bị trừ **sau khi** AI trả lời thành công (`UpdateQuotaAsync`).

### Bước 3 — Dựng ngữ cảnh RAG (`ChatService.BuildContextAsync`)
Chỉ chạy khi có `SelectedDocIds`:

1. **Cache**: key = danh sách docId + hash câu hỏi, TTL 5 phút → câu hỏi lặp lại không tốn quota embedding.
2. **Embedding**: gọi `GeminiService.GetEmbeddingAsync` (model `gemini-embedding-001`, cắt còn 768 chiều).
3. **Semantic search**: `SearchSimilarChunksAsync` trên pgvector, lấy top 20 đoạn.
4. **Rerank**: `RerankChunksAsync` nhờ AI (model `gemini-2.0-flash`, quota **riêng** với model chat, `maxRetries: 1` = fail là bỏ) chấm điểm và chọn top 5. Nếu AI lỗi → fallback `LocalKeywordRerank` (chấm điểm theo từ khoá, chạy cục bộ, không tốn API).
5. Ghép các đoạn thành `contextText` + tạo danh sách `citations` (nguồn trích dẫn).

> ⚠️ Nếu bước embedding lỗi (ví dụ 429): **không** làm hỏng cả câu chat — code sẽ bỏ qua semantic search và dùng thẳng nội dung tài liệu (cắt 3000 ký tự đầu) làm ngữ cảnh.

### Bước 4 — Dựng prompt (`ChatService.BuildPrompt`)
Prompt gửi cho Gemini gồm các phần, theo thứ tự:
1. **Thông tin hệ thống**: gói đang dùng + số lượt còn lại (để AI trả lời được khi người dùng hỏi "tôi còn mấy lượt").
2. **Chỉ thị danh tính**: AI phải tự nhận là "trợ lý AI của ChatEdu", không tiết lộ là Gemini.
3. **Lịch sử hội thoại**: 8 tin nhắn gần nhất, mỗi tin cắt còn 500 ký tự.
4. **Tài liệu liên quan** (nếu có): kèm chỉ thị bắt buộc trích dẫn dạng `[1][2]` cuối câu; nếu `RestrictToDocs` thì cấm dùng kiến thức ngoài.
5. **Câu hỏi hiện tại**.

### Bước 5 — Gọi Gemini streaming (`GeminiService.GenerateStreamingAnswerAsync`)
- Endpoint: `POST .../models/{model}:streamGenerateContent?alt=sse&key=...`
- Đọc từng dòng SSE `data: {...}`, tách text và `yield` từng chunk.
- **Chuỗi model fallback**: thử lần lượt *model chính* → `gemini-flash-lite-latest` → `gemini-3.5-flash` → `gemini-2.0-flash`. Gặp **429/503/500/404** thì chuyển ngay sang model kế tiếp (không ngồi chờ backoff trên model đã hết quota). Lý do: **quota Gemini tính riêng theo từng model**, đổi model là có quota mới.
- Timeout tổng: 90 giây (đặt ở `ChatService`).

Mỗi chunk được đẩy về client qua `ReceiveChunk` → chữ hiện dần trên màn hình.

### Bước 6 — Hoàn tất
- `ChatService` gom toàn bộ chunk thành `replyText`. Nếu rỗng hoặc bắt đầu bằng `⚠️`/`Lỗi khi gọi AI` → coi là lỗi, trả `StreamError`, **không trừ quota, không lưu tin nhắn**.
- Thành công: tạo phiên nếu chưa có (tiêu đề = 22 ký tự đầu câu hỏi), lưu 2 tin nhắn (user + model, citations lưu dạng JSON trong `CitationPayloadJson`), trừ quota, gửi `StreamComplete` (sessionId, title, remaining, citations, reply).

---

## 3. Cấu hình

### File `.env` (gốc dự án — nạp lúc khởi động, **đổi xong phải restart app**)
```env
GEMINI_API_KEY=<key từ Google AI Studio>
GEMINI_MODEL=gemini-3.1-flash-lite
```

### Thứ tự ưu tiên chọn model chat
1. `ModelName` người dùng chọn trên UI (gửi kèm request)
2. Biến môi trường `GEMINI_MODEL`
3. `GeminiAI:Model` trong `appsettings.json`
4. Mặc định cứng trong code: `gemini-3.1-flash-lite`

### Chuỗi model dự phòng (hard-code trong `GeminiService.FallbackModels`)
```
gemini-flash-lite-latest → gemini-3.5-flash → gemini-2.0-flash
```
Muốn đổi: sửa mảng `FallbackModels` đầu file `GeminiService.cs`.

### Các model dùng cho việc khác
- **Embedding**: `gemini-embedding-001` (hard-code trong `GetEmbeddingAsync`)
- **Rerank**: `gemini-2.0-flash` (hard-code trong `ChatService.RerankChunksAsync`) — cố ý dùng model khác model chat để không "ăn" chung quota.

---

## 4. Xử lý lỗi & retry (cơ chế hiện tại)

| Tầng | Cơ chế |
|---|---|
| `PostWithRetryAsync` (blocking) | Retry tối đa 4 lần với backoff 5s/15s/45s cho lỗi 429/503/500; tôn trọng header `Retry-After` nhưng **cap 30 giây** (hết quota ngày thì Google trả Retry-After rất lớn, chờ vô ích) |
| `GenerateAnswerAsync` | Có fallback model. Khi có nhiều model trong chuỗi: mỗi model chỉ gọi **1 lần** rồi chuyển tiếp. `maxRetries: 1` = chế độ fail-nhanh (rerank dùng) — không retry, không fallback |
| `GenerateStreamingAnswerAsync` | Thử lần lượt từng model trong chuỗi, mỗi model 1 lần |
| Rerank lỗi | Fallback `LocalKeywordRerank` (cục bộ, miễn phí) |
| Embedding lỗi | Bỏ semantic search, dùng nội dung tài liệu trực tiếp |
| Message lỗi hiển thị | 429 → "⚠️ Trợ lý AI đang quá tải…" · 503 → "⚠️ Dịch vụ AI tạm thời không khả dụng" · khác → "Lỗi khi gọi AI: {status}" |

---

## 5. 🔧 Hướng dẫn sửa lỗi (Troubleshooting)

### Bước chẩn đoán chung: test API key trực tiếp

Chạy trong Git Bash tại gốc dự án (đọc key từ `.env`):

```bash
KEY=$(grep GEMINI_API_KEY .env | cut -d= -f2 | tr -d '\r')

# 1. Key còn sống không? (200 = OK)
curl -s -o /dev/null -w "%{http_code}\n" \
  "https://generativelanguage.googleapis.com/v1beta/models?key=$KEY"

# 2. Model đang cấu hình còn quota không? (thay tên model nếu cần)
curl -s -w "\nHTTP:%{http_code}\n" -X POST \
  "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite:generateContent?key=$KEY" \
  -H "Content-Type: application/json" \
  -d '{"contents":[{"parts":[{"text":"hi"}]}]}'

# 3. Quét nhanh xem model nào còn dùng được
for m in gemini-3.1-flash-lite gemini-flash-lite-latest gemini-3.5-flash gemini-2.0-flash; do
  code=$(curl -s -o /dev/null -w "%{http_code}" -X POST \
    "https://generativelanguage.googleapis.com/v1beta/models/$m:generateContent?key=$KEY" \
    -H "Content-Type: application/json" -d '{"contents":[{"parts":[{"text":"hi"}]}]}')
  echo "$m -> $code"
done
```

Đọc kết quả: **200** = dùng được · **429** = hết quota model đó · **404** = model không tồn tại/đã khai tử · **503** = Google đang quá tải (thoáng qua) · **400/403** = key sai hoặc bị khoá.

---

### Lỗi 1: "⚠️ Trợ lý AI đang quá tải, vui lòng thử lại sau vài giây" (HTTP 429)

**Nguyên nhân**: hết quota free-tier của Gemini. Quota tính **riêng theo từng model**, có 3 loại: theo phút (RPM), theo ngày (RPD), theo token/phút. Xem body lỗi trả về (lệnh curl số 2 ở trên) — trường `quotaId` cho biết loại nào:
- `...PerDayPerProjectPerModel-FreeTier` → hết quota **ngày** → chờ đến nửa đêm giờ Thái Bình Dương (~14h-15h VN) hoặc **đổi model khác**.
- `...PerMinutePerProjectPerModel-FreeTier` → hết quota **phút** → chờ ~1 phút là hết.

**Cách sửa theo thứ tự**:
1. Chạy lệnh quét model (curl số 3), chọn model trả về 200.
2. Sửa `GEMINI_MODEL` trong `.env` → **restart app**.
3. Nếu tất cả model đều 429: chờ reset ngày, hoặc tạo API key mới từ project Google Cloud khác, hoặc bật billing để nâng quota.
4. Kiểm tra `FallbackModels` trong `GeminiService.cs` còn model sống không — cơ chế fallback chỉ hiệu quả khi trong chuỗi có ít nhất 1 model còn quota.

**Giảm tốc độ "đốt" quota**:
- Mỗi câu hỏi có chọn tài liệu tốn **3 call**: 1 embedding + 1 rerank + 1 chat. Không chọn tài liệu chỉ tốn 1 call.
- Cache ngữ cảnh 5 phút đã giúp câu hỏi lặp lại không tốn embedding/rerank.
- Nếu cần tiết kiệm tối đa: trong `RerankChunksAsync` có thể `return LocalKeywordRerank(query, chunks, topN);` ngay đầu hàm để bỏ hẳn call rerank AI.

### Lỗi 2: "Lỗi khi gọi AI: NotFound - ..." (HTTP 404)

**Nguyên nhân**: model trong `.env` (hoặc model người dùng chọn trên UI) **không tồn tại hoặc đã bị Google khai tử** (ví dụ toàn bộ dòng `gemini-1.5-*` đã chết).

**Cách sửa**: liệt kê model key đang có quyền dùng rồi cập nhật `.env`:
```bash
curl -s "https://generativelanguage.googleapis.com/v1beta/models?key=$KEY&pageSize=100" | grep '"name"'
```

### Lỗi 3: "⚠️ Dịch vụ AI tạm thời không khả dụng" (HTTP 503)

**Nguyên nhân**: server Google quá tải (hay gặp với model mới/hot). Là lỗi **thoáng qua**, code đã tự retry + fallback model. Nếu kéo dài → đổi `GEMINI_MODEL` sang model ít "hot" hơn (dòng `-lite`).

### Lỗi 4: "Lỗi hệ thống: ..." 

**Nguyên nhân**: exception ngoài dự kiến — đọc phần message sau chữ "Lỗi hệ thống:" để biết cụ thể. Hay gặp:
- `GEMINI_API_KEY is not configured` → thiếu key trong `.env` hoặc app không đọc được file `.env` (chú ý: `Program.cs` nạp bằng đường dẫn tương đối `../.env`, phải chạy app từ thư mục `PresentationLayer`).
- Lỗi kết nối PostgreSQL → kiểm tra `DB_CONNECTION_STRING` và service Postgres.
- `Gemini Embedding Error` → quota embedding hết; chat vẫn chạy nhưng không có semantic search (đã có fallback).

### Lỗi 5: Chat không phản hồi gì / chữ không hiện dần

**Nguyên nhân**: SignalR không kết nối được.
1. Mở DevTools (F12) → tab Console/Network, tìm lỗi kết nối `/courseHub`.
2. Kiểm tra `Program.cs` có `AddSignalR()` + `MapHub<SignalRHub>("/courseHub")`.
3. Lỗi 401 khi negotiate → phiên đăng nhập hết hạn, đăng nhập lại (hub có `[Authorize]`).
4. Client vẫn có fallback AJAX non-streaming (`?handler=SendChatMessage`) — nếu cả đường này cũng không chạy thì lỗi ở server, xem log console `dotnet run`.

### Lỗi 6: "Bạn đã dùng hết X câu hỏi..." (OutOfQuota)

Đây **không phải lỗi hệ thống** — là quota nội bộ của app (theo gói hội viên, xem bảng ở mục 2 Bước 2). Người dùng cần: chờ reset (5 giờ / tháng), bật công tắc "Sử dụng lượt dự phòng", hoặc nâng gói. Muốn đổi giới hạn: sửa `GetShortTermLimit` / `GetMonthlyLimit` trong `ChatService.cs`.

### Lỗi 7: Trả lời không kèm trích dẫn / trả lời sai tài liệu

1. Kiểm tra tài liệu đã được **embedding chưa** (bảng `DocumentChunks` có dữ liệu vector chưa) — tài liệu mới upload phải qua bước xử lý chunk + embedding.
2. Nếu `SearchSimilarChunksAsync` trả rỗng → hệ thống fallback dùng 3000 ký tự đầu tài liệu, chất lượng thấp hơn.
3. Bật `RestrictToDocs` để ép AI chỉ dùng tài liệu.

---

## 6. Ghi chú lịch sử sửa lỗi

**2026-07-15 — Sửa lỗi 429 toàn hệ thống chat**:
- Nguyên nhân gốc: `GEMINI_MODEL=gemini-2.0-flash` hết quota **ngày** free-tier; retry backoff vô dụng vì quota ngày không reset trong phiên. Model rerank `gemini-1.5-flash` đã bị Google khai tử (404 mọi call).
- Đã sửa: đổi model chính sang `gemini-3.1-flash-lite`; thêm chuỗi fallback model vào `GeminiService` (blocking + streaming); cap `Retry-After` 30s; rerank chuyển sang `gemini-2.0-flash`; embedding lỗi không còn làm chết cả câu chat.
