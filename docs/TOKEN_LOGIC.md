# 🔢 Logic hạn mức Token AI — Giải thích chi tiết

Tài liệu này giải thích **token đến từ đâu, được đo như thế nào, trừ vào đâu, và code nào chịu trách nhiệm** cho từng bước — để bạn đọc và đối chiếu trực tiếp với source.

---

## 1. Tổng quan luồng (từ câu hỏi đến khi trừ token)

```
Người dùng gửi câu hỏi
        │
        ▼
1. CheckQuotaAsync()          ← kiểm tra CÒN hạn mức không (chưa biết tốn bao nhiêu)
        │  (nếu hết → chặn, không gọi AI)
        ▼
2. BuildContextAsync()        ← semantic search lấy đoạn tài liệu liên quan (RAG)
        ▼
3. BuildPrompt()               ← ghép: system info + lịch sử chat + tài liệu + câu hỏi
        ▼
4. GeminiService.GenerateAnswerAsync() / GenerateStreamingAnswerAsync()
        │  → gọi Google Gemini API, ĐỌC usageMetadata trong response
        ▼
5. AI trả lời xong → biết chính xác đã tốn bao nhiêu token
        ▼
6. UpdateQuotaAsync()          ← TRỪ token thật vào User.ShortTermTokensUsed / MonthlyTokensUsed
   LogTokenUsageAsync()        ← ghi 1 dòng vào bảng TokenUsageLogs (cho thống kê Admin)
```

**Nguyên tắc cốt lõi: kiểm tra trước – trừ sau.** Vì không thể biết trước một câu hỏi sẽ tốn bao nhiêu token (phụ thuộc độ dài tài liệu tìm được + độ dài câu trả lời), hệ thống chỉ **chặn nếu ĐÃ vượt hạn mức từ trước**; token của câu hỏi hiện tại luôn được cho phép chạy xong rồi mới trừ. Nghĩa là câu cuối cùng trước khi hết hạn mức có thể khiến số đã dùng vượt nhẹ qua giới hạn — đây là cách các dịch vụ AI trả phí (OpenAI, Anthropic...) đều làm, vì không có cách nào đếm token chính xác trước khi model chạy xong.

---

## 2. Token "thật" lấy từ đâu?

Google Gemini API trả về một trường `usageMetadata` trong mọi response, ví dụ:

```json
{
  "candidates": [ ... ],
  "usageMetadata": {
    "promptTokenCount": 1523,
    "candidatesTokenCount": 340,
    "thoughtsTokenCount": 0,
    "totalTokenCount": 1863
  }
}
```

- `promptTokenCount` = số token của **toàn bộ prompt gửi lên** (system info + lịch sử chat + tài liệu RAG + câu hỏi) — đây là lý do prompt dài (nhiều tài liệu, nhiều lịch sử) tốn nhiều token dù câu hỏi ngắn.
- `candidatesTokenCount` = số token của **câu trả lời AI sinh ra**.
- `thoughtsTokenCount` = token "suy nghĩ nội bộ" (một số model dòng *thinking* sinh ra trước khi trả lời) — người dùng vẫn bị tính phí cho phần này nên hệ thống cộng luôn vào output.

### Code đọc usageMetadata

📄 [`BussinessLayer/Services/GeminiService.cs:170-179`](BussinessLayer/Services/GeminiService.cs#L170-L179)

```csharp
private static GeminiTokenUsage? ParseUsageMetadata(JsonElement root, string model)
{
    if (!root.TryGetProperty("usageMetadata", out var usage)) return null;
    int promptTokens = usage.TryGetProperty("promptTokenCount", out var pt) ? pt.GetInt32() : 0;
    int outputTokens = usage.TryGetProperty("candidatesTokenCount", out var ct) ? ct.GetInt32() : 0;
    // Cộng cả token "suy nghĩ" (model dòng thinking) nếu có — người dùng vẫn tiêu thụ chúng
    if (usage.TryGetProperty("thoughtsTokenCount", out var tt)) outputTokens += tt.GetInt32();
    if (promptTokens == 0 && outputTokens == 0) return null;
    return new GeminiTokenUsage(model, promptTokens, outputTokens);
}
```

`GeminiTokenUsage` là một `record` đơn giản mang 3 giá trị:

📄 [`BussinessLayer/IServices/IGeminiService.cs:15-18`](BussinessLayer/IServices/IGeminiService.cs#L15-L18)
```csharp
public record GeminiTokenUsage(string Model, int PromptTokens, int OutputTokens)
{
    public int TotalTokens => PromptTokens + OutputTokens;
}
```

### Đọc ở luồng KHÔNG streaming (blocking)

Khi gọi `generateContent`, response về nguyên khối JSON 1 lần → parse ngay:

📄 [`GeminiService.cs:200-211`](BussinessLayer/Services/GeminiService.cs#L200-L211)
```csharp
if (response.IsSuccessStatusCode)
{
    var responseString = await response.Content.ReadAsStringAsync();
    using var result = JsonDocument.Parse(responseString);
    var usage = ParseUsageMetadata(result.RootElement, model);
    if (usage != null) onUsage?.Invoke(usage);   // ← báo ngược về ChatService qua callback
    var cands = result.RootElement.GetProperty("candidates");
    ...
    return parts[0].GetProperty("text").GetString() ?? string.Empty;
}
```

### Đọc ở luồng STREAMING (SSE — chữ hiện dần trên UI)

Đây phức tạp hơn vì response được Google trả về theo từng **chunk SSE** (`data: {...}`), và `usageMetadata` chỉ xuất hiện đầy đủ ở **chunk cuối cùng**. Code giữ lại giá trị mới nhất mỗi lần thấy, sau khi stream kết thúc mới gọi callback:

📄 [`GeminiService.cs:275-312`](BussinessLayer/Services/GeminiService.cs#L275-L312)
```csharp
using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
using var reader = new System.IO.StreamReader(stream);

// usageMetadata xuất hiện dồn tích trong các chunk SSE; chunk cuối chứa tổng chính thức → giữ giá trị cuối cùng
GeminiTokenUsage? lastUsage = null;

while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
{
    string? line = await reader.ReadLineAsync();
    if (string.IsNullOrWhiteSpace(line)) continue;
    if (!line.StartsWith("data: ")) continue;
    var jsonStr = line.Substring("data: ".Length).Trim();
    if (jsonStr == "[DONE]") break;

    string? chunkText = null;
    try
    {
        using var doc = JsonDocument.Parse(jsonStr);
        var root = doc.RootElement;
        lastUsage = ParseUsageMetadata(root, activeModel) ?? lastUsage;   // ← cập nhật mỗi chunk, giữ giá trị cuối
        if (root.TryGetProperty("candidates", out var cands) && ...)
            chunkText = tp.GetString();
    }
    catch { /* bỏ qua chunk lỗi parse */ }

    if (!string.IsNullOrEmpty(chunkText))
        yield return chunkText;   // ← trả từng chữ cho UI hiện dần
}

if (lastUsage != null) onUsage?.Invoke(lastUsage);   // ← chỉ báo usage khi stream đã xong hẳn
```

**Vì sao dùng callback `Action<GeminiTokenUsage>?` chứ không `return`?** Vì phương thức `GenerateStreamingAnswerAsync` là `IAsyncEnumerable<string>` (dùng `yield return` để trả từng chữ) — một hàm generator như vậy không thể `return` thêm giá trị khác. Callback `onUsage` là lối thoát: `ChatService` truyền vào một lambda `u => usage = u`, và `GeminiService` gọi lambda đó đúng 1 lần sau khi biết tổng token chính xác.

### Fallback khi API không trả usageMetadata

Rất hiếm nhưng có thể xảy ra (lỗi mạng giữa chừng, model cũ...). Khi đó `usage == null` → hệ thống **ước lượng thô: 1 token ≈ 4 ký tự**:

📄 [`ChatService.cs:149-151`](BussinessLayer/Services/ChatService.cs#L149-L151) (áp dụng y hệt ở luồng streaming, dòng 230-232)
```csharp
// Token thực tế từ usageMetadata; nếu API không trả về thì ước lượng (ký tự/4)
var promptTokens = usage?.PromptTokens ?? EstimateTokenCount(prompt);
var outputTokens = usage?.OutputTokens ?? EstimateTokenCount(replyText);
```

📄 [`ChatService.cs:574-578`](BussinessLayer/Services/ChatService.cs#L574-L578)
```csharp
private static int EstimateTokenCount(string text)
{
    if (string.IsNullOrEmpty(text)) return 0;
    return (int)Math.Ceiling(text.Length / 4.0); // rough estimate: 4 chars ~ 1 token
}
```

---

## 3. Cái gì "ăn" token — và cái gì KHÔNG

| Lượt gọi AI | Có tính vào quota người dùng? | Model dùng | Lý do |
|---|---|---|---|
| **Chat chính** (`GenerateAnswerAsync` / `GenerateStreamingAnswerAsync`) | ✅ **CÓ** — đây là nguồn duy nhất trừ quota | model người dùng chọn (mặc định `GEMINI_MODEL`) | Đây là giá trị người dùng thực sự nhận được |
| Embedding câu hỏi (`GetEmbeddingAsync`, để tìm chunk liên quan) | ❌ Không | `gemini-embedding-001` | Chi phí hệ thống, không có `usageMetadata` giống dạng chat |
| Rerank chunk (`RerankChunksAsync`) | ❌ Không | `gemini-2.0-flash` riêng | Chỉ là bước xử lý nội bộ để chọn top 5 đoạn tài liệu tốt nhất, người dùng không "thấy" kết quả này trực tiếp |

**Vì sao prompt chat lại to?** Vì `promptTokenCount` tính luôn:
1. Khối `[THÔNG TIN HỆ THỐNG]` (gói + hạn mức còn lại)
2. Khối `[QUAN TRỌNG VỀ DANH TÍNH]` (chỉ thị ẩn danh AI)
3. **Lịch sử hội thoại** — tối đa 8 tin nhắn gần nhất (xem `BuildConversationHistory`, [`ChatService.cs:649-668`](BussinessLayer/Services/ChatService.cs#L649-L668)): 2 tin cuối giữ tới 6.000 ký tự, tin cũ hơn cắt 500 ký tự
4. **Tài liệu liên quan** (`contextText`) — tối đa 5 chunk sau rerank, mỗi chunk có thể vài trăm từ
5. Câu hỏi hiện tại

→ Một câu hỏi *có chọn môn học* (kích hoạt RAG) luôn tốn nhiều `promptTokens` hơn câu hỏi *không chọn môn* (không có khối tài liệu), dù `outputTokens` (câu trả lời) có thể ngắn.

---

## 4. Hạn mức theo gói — định nghĩa & áp dụng

Toàn bộ số hạn mức nằm ở **một nguồn duy nhất** để tránh lệch giữa các service:

📄 [`BussinessLayer/Services/TokenQuota.cs`](BussinessLayer/Services/TokenQuota.cs) (toàn văn)
```csharp
public static class TokenQuota
{
    public const long TokensPerLegacyQuestion = 5_000;

    public static long GetShortTermTokenLimit(string plan) => plan switch
    {
        "Basic" => 50_000,
        "Pro" => 100_000,
        "Ultra" => long.MaxValue,
        _ => 50_000
    };

    public static long GetMonthlyTokenLimit(string plan) => plan switch
    {
        "Basic" => 250_000,
        "Pro" => 2_500_000,
        "Ultra" => long.MaxValue,
        _ => 250_000
    };
}
```

| Gói | Hạn mức 5 giờ | Hạn mức tháng |
|---|---|---|
| Basic (mặc định/free) | 50.000 token | 250.000 token |
| Pro | 100.000 token | 2.500.000 token |
| Ultra | Không giới hạn (`long.MaxValue`) | Không giới hạn |

Hai class dùng chung hằng số này:
- `ChatService.CheckQuotaAsync` — kiểm tra trước khi gọi AI
- `SubscriptionService` — hiển thị % đã dùng ở trang "Gói hội viên" và trang Admin

---

## 5. Nơi lưu số đã dùng — `User` entity

📄 [`DataAccessLayer/Entities/User.cs:38-48`](DataAccessLayer/Entities/User.cs#L38-L48)
```csharp
public long MonthlyTokensUsed { get; set; } = 0;   // số token đã dùng trong tháng
public DateTime? QuotaResetDate { get; set; }       // ngày reset quota (đầu tháng tiếp theo)

public long ShortTermTokensUsed { get; set; } = 0;  // số token đã dùng trong chu kỳ 5 giờ
public DateTime? ShortTermResetDate { get; set; }   // thời điểm reset chu kỳ 5 giờ

public long ExtraTokenQuota { get; set; } = 0;      // số token dự phòng mua thêm (không hết hạn)
public bool UseExtraQuota { get; set; } = false;    // bật/tắt việc tiêu token dự phòng
```

Mỗi user có **2 bộ đếm song song** (5 giờ + tháng) — hết 1 trong 2 là bị chặn, cho đến khi bộ đó reset. Đây là cơ chế chống spam (chặn nhanh trong ngày) + giới hạn tổng chi phí (chặn theo tháng).

Ngoài ra còn bảng **`TokenUsageLogs`** — không dùng để tính quota (chỉ để log/thống kê), mỗi dòng là 1 lượt gọi AI:

📄 [`DataAccessLayer/Entities/TokenUsageLog.cs`](DataAccessLayer/Entities/TokenUsageLog.cs)
```csharp
public class TokenUsageLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Feature { get; set; } = "chat";
    public string? Model { get; set; }        // model Gemini thực tế đã trả lời (có thể khác model yêu cầu do fallback)
    public int PromptTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
    public bool IsEstimated { get; set; }     // true nếu dùng số ước lượng (ký tự/4) thay vì usageMetadata thật
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

---

## 6. Bước 1 — Kiểm tra quota TRƯỚC khi gọi AI

📄 [`ChatService.CheckQuotaAsync`, `ChatService.cs:264-329`](BussinessLayer/Services/ChatService.cs#L264-L329)

Logic từng bước:

```csharp
private async Task<(...)> CheckQuotaAsync(int userId)
{
    var user = await _userRepository.GetUserByIdAsync(userId);
    ...
    // 1. Reset các bộ đếm nếu đã qua hạn (5h hoặc 30 ngày) — xem mục 8
    await _subscriptionService.CheckAndUpdateQuotaAsync(userId);
    user = await _userRepository.GetUserByIdAsync(userId) ?? user;

    // 2. Xác định gói hiệu lực (nếu hết hạn subscription thì rơi về Basic)
    var planActive = user.SubscriptionPlan == "Basic" || user.SubscriptionPlan == "Free" ||
                     (user.SubscriptionExpiry.HasValue && user.SubscriptionExpiry.Value >= now);
    effectivePlan = planActive ? user.SubscriptionPlan : "Basic";

    // 3. Lấy hạn mức từ TokenQuota, tính còn lại
    var limitShort = TokenQuota.GetShortTermTokenLimit(effectivePlan);
    var limitMonth = TokenQuota.GetMonthlyTokenLimit(effectivePlan);
    remainingShortTokens = limitShort - user.ShortTermTokensUsed;
    remainingMonthTokens = limitMonth - user.MonthlyTokensUsed;

    // 4. Đã dùng >= hạn mức (tháng HOẶC 5h) → outOfStandardQuota = true
    if (limitMonth != long.MaxValue && user.MonthlyTokensUsed >= limitMonth) { outOfStandardQuota = true; ... }
    else if (limitShort != long.MaxValue && user.ShortTermTokensUsed >= limitShort) { outOfStandardQuota = true; ... }

    // 5. Hết quota chuẩn nhưng có token dự phòng VÀ đã bật công tắc → cho phép, dùng dự phòng
    if (outOfStandardQuota)
    {
        if (user.UseExtraQuota && user.ExtraTokenQuota > 0)
            isUsingExtraQuota = true;
        else
            return (... Success = false, OutOfQuota = true ...);  // ← CHẶN, không gọi AI
    }

    return (... Success = true ...);  // ← Cho phép tiếp tục
}
```

**Điểm quan trọng:** hàm này chỉ trả `bool isUsingExtraQuota` — nó **không hề biết** câu hỏi sắp tới tốn bao nhiêu token, vì AI chưa chạy. Nó chỉ trả lời được câu "tính đến TRƯỚC câu hỏi này, user còn quyền hỏi không?".

---

## 7. Bước 2 — Trừ token THẬT sau khi AI trả lời xong

Sau khi `GenerateAnswerAsync`/`GenerateStreamingAnswerAsync` chạy xong và biết số token chính xác:

📄 [`ChatService.cs:149-155`](BussinessLayer/Services/ChatService.cs#L149-L155) *(luồng blocking; luồng streaming giống hệt ở dòng 230-236)*
```csharp
var promptTokens = usage?.PromptTokens ?? EstimateTokenCount(prompt);
var outputTokens = usage?.OutputTokens ?? EstimateTokenCount(replyText);

await SaveMessagesAndUpdateSessionAsync(session, request.Message, replyText, citations, outputTokens);
await UpdateQuotaAsync(user, isUsingExtraQuota, promptTokens + outputTokens);
await LogTokenUsageAsync(userId, usage, promptTokens, outputTokens);
```

**`UpdateQuotaAsync` — nơi thực sự trừ số:**

📄 [`ChatService.cs:523-534`](BussinessLayer/Services/ChatService.cs#L523-L534)
```csharp
private async Task UpdateQuotaAsync(User? user, bool isUsingExtraQuota, long tokensUsed)
{
    if (user == null) return;
    if (isUsingExtraQuota)
        user.ExtraTokenQuota = Math.Max(0, user.ExtraTokenQuota - tokensUsed);   // trừ vào kho dự phòng
    else
    {
        user.ShortTermTokensUsed += tokensUsed;   // cộng vào bộ đếm 5 giờ
        user.MonthlyTokensUsed += tokensUsed;     // VÀ cộng vào bộ đếm tháng (song song, không loại trừ nhau)
    }
    await _userRepository.UpdateUserAsync(user);
}
```

Lưu ý: khi **không** dùng dự phòng, `tokensUsed` được cộng vào **cả 2** bộ đếm cùng lúc (5 giờ và tháng) — vì cả 2 giới hạn phải giám sát đồng thời, không phải "hoặc cái này hoặc cái kia".

**`LogTokenUsageAsync` — ghi log cho thống kê Admin (không ảnh hưởng quota):**

📄 [`ChatService.cs:536-554`](BussinessLayer/Services/ChatService.cs#L536-L554)
```csharp
private async Task LogTokenUsageAsync(int userId, GeminiTokenUsage? usage, int promptTokens, int outputTokens)
{
    try
    {
        await _tokenUsageLogRepository.AddAsync(new TokenUsageLog
        {
            UserId = userId,
            Feature = "chat",
            Model = usage?.Model,                 // model thật đã trả lời (có thể là model fallback)
            PromptTokens = promptTokens,
            OutputTokens = outputTokens,
            TotalTokens = promptTokens + outputTokens,
            IsEstimated = usage == null,           // true = dùng số ước lượng, không phải usageMetadata thật
            CreatedAt = DateTime.UtcNow
        });
    }
    catch { /* không chặn luồng chat nếu ghi log lỗi */ }
}
```

Bảng `TokenUsageLogs` này chính là nguồn dữ liệu cho trang **Admin → Thống kê** (biểu đồ token theo tháng/năm, bảng chi tiết theo user) — xem `StatisticsService.GetAdminStatisticsAsync`.

---

## 8. Reset hạn mức theo thời gian

Hai nơi cùng có trách nhiệm reset (để không bao giờ bị bỏ sót dù user không hoạt động lâu):

1. **Lazy reset khi user hỏi tiếp** — `SubscriptionService.CheckAndUpdateQuotaAsync`, được `ChatService.CheckQuotaAsync` gọi ở đầu mỗi lượt hỏi:

📄 [`SubscriptionService.cs:113-145`](BussinessLayer/Services/SubscriptionService.cs#L113-L145) (rút gọn)
```csharp
public async Task<bool> CheckAndUpdateQuotaAsync(int userId)
{
    var user = await _userRepository.GetUserByIdAsync(userId);
    var now = DateTime.UtcNow;

    if (user.QuotaResetDate == null || now >= user.QuotaResetDate.Value)
    {
        user.QuotaResetDate = now.AddDays(30);
        user.MonthlyTokensUsed = 0;      // ← reset bộ đếm THÁNG
    }
    if (user.ShortTermResetDate == null || now >= user.ShortTermResetDate.Value)
    {
        user.ShortTermResetDate = now.AddHours(5);
        user.ShortTermTokensUsed = 0;    // ← reset bộ đếm 5 GIỜ
    }
    ...
}
```

2. **Background job quét toàn hệ thống** (phòng trường hợp user không hỏi gì nên lazy reset không kích hoạt, nhưng Admin cần số liệu đúng ngay) — `QuotaResetBackgroundService`, chạy mỗi 1 phút, quét toàn bộ user đã quá hạn và reset về `null`/`0`.

**Token dự phòng (`ExtraTokenQuota`) KHÔNG BAO GIỜ tự reset** — chỉ giảm dần khi dùng, hoặc tăng lên khi mua thêm gói addon (`SubscriptionService.ProcessPaymentSuccessAsync`, cộng `AddonPackage.QuotaAmount` vào `ExtraTokenQuota`).

---

## 9. Tóm tắt "1 câu hỏi tốn bao nhiêu token"

Không có số cố định — phụ thuộc:

| Yếu tố | Ảnh hưởng |
|---|---|
| Có chọn môn học / bật RAG? | Có → cộng thêm token của tối đa 5 đoạn tài liệu (`contextText`) vào `promptTokens` |
| Lịch sử hội thoại dài? | 8 tin nhắn gần nhất luôn được gửi kèm → phiên chat dài dần sẽ tốn `promptTokens` nhiều hơn mỗi câu |
| Độ dài câu trả lời AI | Quyết định `outputTokens` — câu trả lời dài (giải thích chi tiết, có bảng...) tốn nhiều hơn |
| Model đang dùng | Không đổi cách tính, nhưng nếu bị fallback qua model khác (xem `GeminiService.BuildModelChain`) thì `usage.Model` ghi lại đúng model thực sự trả lời |

Theo ước tính ghi trong `TokenQuota.TokensPerLegacyQuestion` (dùng khi migrate dữ liệu cũ), một câu hỏi có RAG bật tốn **~5.000 token** — đây chỉ là số tham khảo, số thật luôn lấy từ `usageMetadata`.
