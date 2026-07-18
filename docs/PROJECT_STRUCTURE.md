# 🗂️ Cấu trúc thư mục dự án ChatBot_EduAI

Dự án xây dựng theo **kiến trúc 3 lớp (3-Layer Architecture)**: Giao diện → Nghiệp vụ → Dữ liệu. Quy tắc phụ thuộc **một chiều từ trên xuống**:

```
PresentationLayer  →  BussinessLayer  →  DataAccessLayer  →  PostgreSQL
   (giao diện)          (nghiệp vụ)        (truy cập DB)
```

Lớp dưới **không bao giờ** được tham chiếu ngược lên lớp trên (ví dụ: `DataAccessLayer` không được `using BussinessLayer`).

---

## Cây thư mục tổng quan

```
ChatBot_EduAI/
├── ChatbotAI.sln              # File solution — mở bằng Visual Studio / Rider
├── .env                       # ⚙️ Biến môi trường: DB, Gemini API key, SMTP, cổng thanh toán
├── README.md                  # Giới thiệu dự án
├── QuickSetup.md              # Hướng dẫn cài đặt nhanh
├── TaskAssignment.md          # Phân công công việc nhóm
├── CHAT_FEATURE.md            # 📖 Tài liệu chi tiết tính năng Chat AI + sửa lỗi
├── PROJECT_STRUCTURE.md       # 📄 File này
├── images/                    # Ảnh dùng cho README/tài liệu
│
├── PresentationLayer/         # 🖥️ LỚP GIAO DIỆN (ASP.NET Core Razor Pages)
├── BussinessLayer/            # ⚙️ LỚP NGHIỆP VỤ (class library)
└── DataAccessLayer/           # 💾 LỚP TRUY CẬP DỮ LIỆU (EF Core + PostgreSQL)
```

---

## 1. 🖥️ PresentationLayer — Lớp giao diện

Project web chính (ASP.NET Core Razor Pages), là **project khởi chạy** (`dotnet run` từ thư mục này). Nhiệm vụ: nhận request từ trình duyệt, gọi service ở BussinessLayer, trả về HTML/JSON.

```
PresentationLayer/
├── Program.cs                 # 🔑 Điểm khởi động: nạp .env, đăng ký DI (services, repositories),
│                              #    cấu hình auth cookie, SignalR, map hub /courseHub
├── appsettings.json           # Cấu hình phụ (logging...) — cấu hình nhạy cảm nằm ở .env
│
├── Pages/                     # Các trang Razor (.cshtml = giao diện, .cshtml.cs = code-behind)
│   ├── Index.cshtml           # Trang chủ
│   ├── Auth/                  # Đăng nhập, đăng ký, quên mật khẩu
│   ├── Chat/                  # 💬 Trang Chat AI (UI + các handler AJAX: sessions, quota, models...)
│   ├── Student/               # Trang dành cho sinh viên (làm quiz, xem điểm...)
│   ├── StudentDocument/       # Sinh viên xem/tra cứu tài liệu
│   ├── Lecturer/              # Trang giảng viên: upload tài liệu, ngân hàng câu hỏi,
│   │                          #   sinh câu hỏi bằng AI (AIGenerateQuestions)
│   ├── Admin/                 # Trang quản trị: dashboard, quản lý user, môn học,
│   │                          #   gói hội viên, lịch sử thanh toán
│   ├── Subscription/          # Trang mua/nâng cấp gói hội viên
│   ├── Payment/               # Trang thanh toán + callback từ cổng thanh toán
│   ├── PaymentHistory/        # Lịch sử giao dịch của người dùng
│   └── Shared/                # Layout chung (_Layout, sidebar theo role...)
│
├── Controllers/               # API controller thuần (không có giao diện)
│   ├── SePayWebhookController.cs   # Nhận webhook báo tiền về từ SePay
│   └── UserController.cs           # API thao tác user
│
├── SignalR/
│   └── SignalR.cs             # 📡 SignalRHub (/courseHub): chat streaming realtime
│                              #    + đẩy thông báo realtime (thanh toán, cập nhật admin)
│
├── ViewModels/                # Model riêng cho view (Admin/, Auth/, Lecturer/)
│                              #   — khác DTO: chỉ phục vụ hiển thị trang
│
├── wwwroot/                   # File tĩnh trình duyệt tải trực tiếp
│   ├── css/                   # Stylesheet
│   ├── js/                    # JavaScript (site.js...)
│   ├── images/                # Hình ảnh giao diện
│   └── files/                 # 📁 File tài liệu người dùng upload (PDF, DOCX...)
│
└── Properties/                # launchSettings.json — cổng chạy, profile debug
```

**Khi nào sửa ở đây**: đổi giao diện, thêm trang mới, thêm endpoint AJAX, sửa luồng SignalR, sửa đăng ký DI trong `Program.cs`.

---

## 2. ⚙️ BussinessLayer — Lớp nghiệp vụ

Class library chứa **toàn bộ logic nghiệp vụ**: quota, chấm điểm quiz, gọi AI, xử lý thanh toán... Không biết gì về HTTP/giao diện, không truy vấn DB trực tiếp (đi qua repository của DataAccessLayer).

```
BussinessLayer/
├── IServices/                 # 📋 Interface của các service (hợp đồng)
│   ├── IChatService.cs, IGeminiService.cs, IDocumentService.cs,
│   ├── IAuthService.cs, IUserService.cs, IEmailService.cs,
│   ├── IPaymentService.cs, ISubscriptionService.cs, ...
│
├── Services/                  # 🧠 Cài đặt nghiệp vụ (file quan trọng nhất của dự án)
│   ├── ChatService.cs               # Điều phối chat: quota → RAG → prompt → lưu tin nhắn
│   ├── GeminiService.cs             # Gọi Google Gemini API (trả lời, streaming, embedding,
│   │                                #   retry + fallback model khi 429)
│   ├── AIQuizGeneratorService.cs    # Sinh câu hỏi trắc nghiệm từ tài liệu bằng AI
│   ├── DocumentService.cs           # Quản lý tài liệu: upload, chunk, embedding
│   ├── FileTextExtractorService.cs  # Trích xuất text từ PDF/DOCX/PPTX
│   ├── AuthService.cs               # Đăng nhập/đăng ký, hash mật khẩu
│   ├── UserService.cs               # Quản lý người dùng
│   ├── EmailService.cs              # Gửi mail (SMTP Gmail — quên mật khẩu, thông báo)
│   ├── SubscriptionService.cs       # Gói hội viên, quota, lượt dự phòng
│   ├── SubscriptionPlanService.cs   # CRUD các gói (Basic/Pro/Ultra)
│   ├── PaymentService.cs            # Điều phối thanh toán qua các gateway
│   ├── PaymentHistoryService.cs     # Lịch sử giao dịch
│   ├── QuizService.cs               # Làm quiz, chấm điểm
│   ├── QuestionBankService.cs       # Ngân hàng câu hỏi của giảng viên
│   ├── SubjectService.cs            # Môn học, chương
│   ├── DocumentActivityLogService.cs# Log hoạt động trên tài liệu
│   └── QuotaResetBackgroundService.cs # ⏰ Background service tự reset quota theo chu kỳ
│
├── DTOs/                      # 📦 Data Transfer Object — đối tượng truyền dữ liệu giữa các lớp
│   ├── ChatDto.cs             #   (ChatRequestDto, ChatResponseDto, CitationDto...)
│   ├── LoginDto.cs, RegisterDto.cs, UserDto.cs, DocumentDto.cs,
│   ├── QuizDto.cs, SubscriptionPlanDto.cs, PaymentRequest.cs, ...
│   # Mục đích: không trả Entity (bảng DB) thẳng ra giao diện
│
├── Gateways/                  # 💳 Tích hợp các cổng thanh toán
│   ├── PaymentGatewayFactory.cs     # Factory chọn gateway theo tên (VNPay/PayOS/SePay)
│   ├── VNPayGateway.cs, PayOSGateway.cs, SePayGateway.cs
├── IGateways/                 # Interface của gateway
│
└── Helpers/
    └── VNPayLibrary.cs        # Hàm tiện ích ký/verify chữ ký VNPay
```

**Khi nào sửa ở đây**: đổi logic quota, sửa cách gọi AI (model, retry, prompt), thêm nghiệp vụ mới, thêm cổng thanh toán mới (tạo gateway mới + đăng ký vào factory).

---

## 3. 💾 DataAccessLayer — Lớp truy cập dữ liệu

Class library làm việc trực tiếp với **PostgreSQL** qua Entity Framework Core (kèm extension **pgvector** cho tìm kiếm ngữ nghĩa).

```
DataAccessLayer/
├── ApplicationDbContext.cs    # 🔑 DbContext: khai báo các DbSet, cấu hình quan hệ bảng,
│                              #    cấu hình cột vector (pgvector)
│
├── Entities/                  # 🏛️ Entity = ánh xạ 1-1 với bảng trong DB
│   ├── User.cs                     # Người dùng (role, gói, các bộ đếm quota)
│   ├── ChatSession.cs, ChatMessage.cs   # Phiên chat & tin nhắn (kèm citations JSON)
│   ├── Document.cs, DocumentChunk.cs    # Tài liệu & các đoạn đã cắt + vector embedding
│   ├── Subject.cs, Chapter.cs           # Môn học, chương
│   ├── Quiz.cs, QuizQuestion.cs, QuizAnswer.cs, QuizAttempt.cs  # Hệ thống quiz
│   ├── QuestionBank.cs                  # Ngân hàng câu hỏi
│   ├── SubscriptionPlan.cs, AddonPackage.cs  # Gói hội viên, gói lượt hỏi thêm
│   ├── PaymentTransaction.cs            # Giao dịch thanh toán
│   ├── AIGenerationLog.cs               # Log các lần sinh nội dung bằng AI
│   └── DocumentActivityLog.cs           # Log hoạt động tài liệu
│
├── IRepositories/             # 📋 Interface repository
│
├── Repositories/              # 🗃️ Cài đặt truy vấn DB (LINQ/EF Core)
│   ├── ChatRepository.cs            # CRUD phiên chat, tin nhắn, phân trang
│   ├── DocumentRepository.cs        # Tài liệu + SearchSimilarChunksAsync (pgvector)
│   ├── UserRepository.cs, QuizRepository.cs, SubjectRepository.cs,
│   ├── PaymentTransactionRepository.cs, SubscriptionPlanRepository.cs, ...
│
└── Migrations/                # 🧬 Lịch sử thay đổi schema DB (EF Core tự sinh)
                               #   Tạo mới:  dotnet ef migrations add <Ten>
                               #   Áp dụng:  dotnet ef database update
```

**Khi nào sửa ở đây**: thêm/sửa bảng (sửa Entity + tạo migration), thêm câu truy vấn mới (thêm method vào repository + interface).

---

## 4. Quy ước khi thêm tính năng mới

Ví dụ muốn thêm tính năng "Ghi chú cá nhân", đi theo đúng chiều 3 lớp:

| Bước | Lớp | Việc cần làm |
|---|---|---|
| 1 | DataAccessLayer | Tạo `Entities/Note.cs` → khai báo `DbSet<Note>` trong `ApplicationDbContext` → chạy migration |
| 2 | DataAccessLayer | Tạo `IRepositories/INoteRepository.cs` + `Repositories/NoteRepository.cs` |
| 3 | BussinessLayer | Tạo `DTOs/NoteDto.cs` + `IServices/INoteService.cs` + `Services/NoteService.cs` |
| 4 | PresentationLayer | Tạo trang `Pages/Note/` gọi service |
| 5 | PresentationLayer | **Đăng ký DI** trong `Program.cs`: `AddScoped<INoteRepository, NoteRepository>()` và `AddScoped<INoteService, NoteService>()` — quên bước này sẽ lỗi runtime "Unable to resolve service" |

### Mẹo tìm code nhanh

- Lỗi hiện trên giao diện → tìm chuỗi thông báo lỗi bằng tính năng search toàn solution, sẽ ra đúng file nghiệp vụ.
- Liên quan AI/Gemini → `BussinessLayer/Services/GeminiService.cs`, `ChatService.cs`, `AIQuizGeneratorService.cs`.
- Liên quan tiền/thanh toán → `BussinessLayer/Gateways/` + `PaymentService.cs` + `Controllers/SePayWebhookController.cs`.
- Liên quan cấu hình/khởi động/DI → `PresentationLayer/Program.cs` + `.env`.
- Realtime (chat streaming, thông báo) → `PresentationLayer/SignalR/SignalR.cs`.

> ℹ️ Lưu ý: tên lớp nghiệp vụ trong dự án viết là **"BussinessLayer"** (thừa một chữ "s" so với từ đúng "Business") — đây là tên đã cố định trong solution, cứ giữ nguyên, đừng đổi vì sẽ vỡ toàn bộ namespace và reference.
