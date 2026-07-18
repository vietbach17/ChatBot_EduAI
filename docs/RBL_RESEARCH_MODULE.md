# Module 3 — Nghiên cứu (RBL): Kế hoạch triển khai

> Mục tiêu: So sánh **RAG vs fine-tuned model**, benchmark **chunking strategy**, benchmark **embedding model**, và **dashboard** hiển thị kết quả — dựa trên chính hệ thống ChatBot_EduAI đang có.

## 0. Hiện trạng hệ thống (điểm xuất phát)

| Thành phần | Hiện tại |
|---|---|
| Embedding | Gemini `gemini-embedding-001`, vector 768 chiều |
| Lưu trữ vector | PostgreSQL + pgvector, cột `vector(768)` trong `DocumentChunk` |
| Chunking | Context-aware: ~300 từ/chunk, overlap 50 từ (`DocumentService.SplitTextByContext`) |
| Retrieval | Cosine distance, top-5, lọc theo `SubjectId` (`DocumentRepository.SearchSimilarChunksAsync`) |
| LLM trả lời | Gemini (generateContent / streaming) |

→ Đây chính là **cấu hình baseline** của mọi thực nghiệm: `gemini-embedding-001 + context-aware 300/50 + top-5 + Gemini`.

---

## 1. Nền tảng chung: bộ dữ liệu đánh giá (Golden Dataset) — LÀM ĐẦU TIÊN

Mọi benchmark đều cần một bộ câu hỏi có đáp án chuẩn. Không có nó thì không đo được gì.

**Cách làm** (demo 1 môn nên khối lượng nhỏ):
1. Chọn 1 môn đã index đủ tài liệu (ví dụ 5–10 file PDF/DOCX).
2. Tạo **30–50 câu hỏi** kèm:
   - `question`: câu hỏi (tiếng Việt, giống sinh viên hỏi thật)
   - `groundTruthAnswer`: đáp án chuẩn (viết tay hoặc nhờ Gemini sinh rồi duyệt tay)
   - `relevantChunkRef`: đoạn tài liệu chứa đáp án (ghi theo `DocumentId` + đoạn văn bản gốc, **không** ghi theo `ChunkId` vì chunk thay đổi theo strategy)
3. Có thể dùng Gemini sinh nháp câu hỏi từ tài liệu (đã có sẵn `AIQuizGeneratorService` làm mẫu), sau đó **người duyệt lại từng câu**.

**Lưu trữ**: bảng mới `EvalQuestion` hoặc đơn giản là file `eval-dataset.json` trong repo (dễ version, dễ sửa — khuyến nghị dùng JSON cho đồ án).

---

## 2. Benchmark Chunking Strategy

### Các strategy nên so sánh (4 cái là đủ đẹp)

| # | Strategy | Tham số |
|---|---|---|
| 1 | Fixed-size, không overlap | 300 từ |
| 2 | Fixed-size + overlap | 300 từ / overlap 50 |
| 3 | **Context-aware + overlap (baseline hiện tại)** | ngắt theo câu/đoạn, 300/50 |
| 4 | Chunk nhỏ / chunk to (biến thể kích thước) | 150 từ và 600 từ |

### Cách chạy thực nghiệm

Vì corpus demo nhỏ (1 môn), **không cần đụng vào bảng `DocumentChunk` production**. Tạo pipeline riêng:

```
Văn bản gốc (Document.Content, đã có sẵn trong DB)
   → chunk theo từng strategy
   → embed từng chunk (dùng model baseline gemini-embedding-001)
   → lưu vào bảng ExperimentChunk (kèm ExperimentRunId)
   → với mỗi câu hỏi trong Golden Dataset: embed câu hỏi → tìm top-k → chấm điểm
```

### Metrics (retrieval-level — rẻ, không tốn API sinh câu trả lời)

- **Hit@k** (k=1,3,5): tỉ lệ câu hỏi mà chunk đúng nằm trong top-k. *Cách xác định "chunk đúng": chunk chứa ≥ X% nội dung của `relevantChunkRef` (so khớp chuỗi/overlap từ), vì chunk boundary khác nhau giữa các strategy.*
- **MRR** (Mean Reciprocal Rank): trung bình 1/vị trí của chunk đúng đầu tiên.
- **Số chunk tạo ra & chi phí embed**: strategy nào tạo nhiều chunk → tốn nhiều lượt gọi embedding hơn.
- (Tuỳ chọn) **Answer quality end-to-end** trên 10–15 câu: cho Gemini trả lời với context của từng strategy, chấm bằng LLM-as-judge (mục 4).

---

## 3. Benchmark Embedding Model

### Models so sánh

| Model | Chiều | Cách gọi | Chi phí |
|---|---|---|---|
| `gemini-embedding-001` (baseline) | 768 | Đã có sẵn trong `GeminiService` | Free tier |
| `multilingual-e5-base` | 768 | Python service local (sentence-transformers) | Miễn phí |
| `bge-m3` (BAAI) | 1024 | Python service local | Miễn phí |
| `PhoBERT-base` | 768 | Python service local (mean pooling) | Miễn phí |
| `text-embedding-3-small` (OpenAI) | 1536 | REST API | ~$0.02/1M token — với corpus demo chỉ vài xu, hoặc **bỏ qua nếu không có key** |

### Vấn đề kỹ thuật quan trọng: số chiều khác nhau

Cột pgvector hiện cố định `vector(768)` → **không nhét bge-m3 (1024) / OpenAI (1536) vào được**.

**Giải pháp khuyến nghị (đơn giản nhất cho đồ án)**: corpus benchmark rất nhỏ (vài trăm chunk) → **không dùng pgvector cho thực nghiệm**. Lưu embedding dạng `float[]` serialize JSON trong bảng `ExperimentChunk` (cột `text`), load hết vào RAM và tính cosine similarity bằng C# thuần. Vài trăm vector thì tính trong <1ms, khỏi migration, khỏi lo số chiều.

### Python sidecar cho model local

Các model e5/bge/PhoBERT không có REST API sẵn → dựng 1 service FastAPI nhỏ (~50 dòng):

```python
# embed_server.py  —  pip install fastapi uvicorn sentence-transformers
from fastapi import FastAPI
from sentence_transformers import SentenceTransformer
app = FastAPI()
models = {}  # lazy load: "e5" -> intfloat/multilingual-e5-base, "bge" -> BAAI/bge-m3, ...

@app.post("/embed")
def embed(body: dict):          # { "model": "e5", "texts": ["..."] }
    m = get_model(body["model"])
    return {"vectors": m.encode(body["texts"]).tolist()}
```

C# gọi qua `HttpClient` như gọi Gemini. Chạy local bằng `uvicorn embed_server:app`. *Lưu ý: e5 cần prefix `query: ` / `passage: `; PhoBERT cần tách từ tiếng Việt (dùng `pyvi`) để đúng chuẩn — ghi rõ trong báo cáo.*

### Cách chạy & metrics

Giống mục 2 nhưng đảo biến: **cố định chunking = baseline (context-aware 300/50)**, thay embedding model. Metrics: Hit@k, MRR, **latency embed/query (ms)**, chi phí. Mỗi model một `ExperimentRun`.

> Nguyên tắc chung của cả 2 benchmark: **mỗi lần chỉ thay 1 biến**, mọi thứ khác giữ nguyên baseline.

---

## 4. So sánh RAG vs Fine-tuned Model

Đây là phần "nghiên cứu" nhất và dễ sa lầy nhất. Khuyến nghị làm ở mức đồ án:

### Chuẩn bị
1. Từ tài liệu môn học, sinh **100–200 cặp Q&A** làm training set (dùng Gemini sinh, duyệt nhanh), **tách riêng** khỏi 30–50 câu test của Golden Dataset (tuyệt đối không trùng — data leakage).
2. Fine-tune một model theo 1 trong 2 đường:
   - **Đường A (khuyến nghị — không cần GPU)**: OpenAI fine-tuning `gpt-4o-mini` (vài đô cho dataset cỡ này), hoặc Gemini tuned model qua Google AI Studio nếu tài khoản còn hỗ trợ.
   - **Đường B (nếu nhóm có người thích ML)**: LoRA fine-tune model nhỏ (Qwen2.5-1.5B/3B) trên Google Colab free, phục vụ inference qua chính Python sidecar ở mục 3.

### 3 cấu hình đem so trên cùng bộ test

| Cấu hình | Mô tả |
|---|---|
| **Base model (no RAG)** | Gemini trả lời trực tiếp, không context — làm mốc dưới |
| **RAG (hệ thống hiện tại)** | Baseline pipeline |
| **Fine-tuned (no RAG)** | Model đã tune trả lời trực tiếp |

### Metrics

- **Answer correctness**: LLM-as-judge — đưa Gemini (model mạnh, ví dụ `gemini-2.5-pro`) câu hỏi + đáp án chuẩn + câu trả lời của từng cấu hình, chấm 1–5 kèm lý do. Chấm **blind** (không cho judge biết câu trả lời đến từ cấu hình nào).
- **Faithfulness / hallucination**: đếm câu trả lời bịa thông tin không có trong tài liệu (judge chấm).
- **Khả năng trích dẫn nguồn**: RAG có citation, fine-tuned không — điểm bán chất lượng quan trọng.
- **Chi phí & công sức cập nhật**: thêm 1 tài liệu mới → RAG chỉ cần re-index vs fine-tuned phải train lại. Đây là kết luận "ăn tiền" của báo cáo.
- Latency mỗi cấu hình.

---

## 5. Thiết kế kỹ thuật trong codebase

### Entities mới (DataAccessLayer)

```
ExperimentRun      : Id, Name, Type (Chunking|Embedding|RagVsFinetune),
                     ConfigJson (strategy/model/tham số), CreatedAt, Status
ExperimentChunk    : Id, ExperimentRunId, DocumentId, Content, EmbeddingJson (text), OrderIndex
ExperimentResult   : Id, ExperimentRunId, QuestionId, Metric (HitAt5|MRR|Judge|LatencyMs|...),
                     Value (double), DetailJson (top-k trả về, câu trả lời, lý do judge...)
```

### Services mới (BussinessLayer)

- `IEmbeddingProvider` + các implementation: `GeminiEmbeddingProvider` (wrap code sẵn có), `LocalPythonEmbeddingProvider`, `OpenAIEmbeddingProvider` — cùng interface `Task<float[]> EmbedAsync(string text, bool isQuery)`.
- `ChunkingStrategyFactory`: trả về hàm chunk theo tên strategy (tái sử dụng `SplitTextByContext`, thêm biến thể fixed-size).
- `BenchmarkService`: nhận `ExperimentRun` → chạy pipeline (chunk → embed → retrieve → chấm) → ghi `ExperimentResult`. Chạy nền bằng `BackgroundService` (đã có mẫu `QuotaResetBackgroundService`) + báo tiến độ qua SignalR (đã có hub).
- `LlmJudgeService`: gọi Gemini chấm điểm câu trả lời.

### Dashboard (PresentationLayer)

- Trang mới `Pages/Research/Index.cshtml` (role Admin/Lecturer):
  - Form tạo experiment: chọn loại, chọn tham số → nút "Chạy" → progress bar realtime (SignalR).
  - **Bar chart** Hit@1/3/5 & MRR theo strategy/model, **bar chart** latency, **bảng** chi tiết từng câu hỏi (drill-down xem top-k chunks trả về, câu trả lời từng cấu hình đặt cạnh nhau).
  - Dùng **Chart.js** (CDN) — nhẹ, hợp Razor Pages, không cần build step.
  - Nút export CSV để bỏ vào báo cáo.

---

## 6. Lộ trình đề xuất (theo độ ưu tiên)

| Bước | Việc | Ước lượng |
|---|---|---|
| 1 | Golden Dataset 30–50 câu (JSON) | 0.5–1 ngày |
| 2 | Entities + `BenchmarkService` khung + trang Research trống | 1 ngày |
| 3 | **Benchmark chunking** (chỉ cần Gemini embedding sẵn có → chạy được sớm nhất) | 1–1.5 ngày |
| 4 | Python sidecar + **benchmark embedding** (e5, bge-m3, PhoBERT; OpenAI nếu có key) | 1.5–2 ngày |
| 5 | Dashboard chart + drill-down + export CSV | 1–1.5 ngày |
| 6 | Sinh training set + fine-tune + **RAG vs fine-tuned** + LLM-as-judge | 2–3 ngày |

Nếu thiếu thời gian: bước 3 + 4 + 5 là lõi bắt buộc; bước 6 có thể thu nhỏ (bỏ fine-tune thật, chỉ so RAG vs base-model-no-RAG rồi *thảo luận* fine-tuning trong báo cáo — nhưng nếu đề bài chấm điểm mục này thì nên làm Đường A cho nhanh).

## 7. Bẫy cần tránh

- **Đừng** benchmark trên chính bảng `DocumentChunk` production — tách riêng `ExperimentChunk`.
- **Đừng** để câu test trùng/na ná câu train của fine-tuning.
- Free tier Gemini có **rate limit** — thêm delay/retry trong `BenchmarkService`, chạy nền chứ đừng chạy trong request.
- E5 cần prefix `query:`/`passage:`, PhoBERT cần word-segmentation — sai cái này là kết quả benchmark của model đó vô nghĩa.
- Chấm LLM-as-judge phải **blind** và nêu rõ prompt chấm trong báo cáo để có tính lặp lại.
