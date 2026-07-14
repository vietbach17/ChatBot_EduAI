# 🤖 ChatEdu AI - Hệ Thống Trợ Lý Học Tập Thông Minh

ChatEdu AI là một nền tảng hỗ trợ học tập tích hợp Trợ lý Trí tuệ Nhân tạo (Generative AI), giúp sinh viên tra cứu, ôn tập và hỏi đáp kiến thức dựa trên nguồn tài liệu chuẩn do Giảng viên cung cấp.

---

## 🛠 Công Nghệ Sử Dụng

- **Backend:** C# / .NET 8, ASP.NET Core Razor Pages
- **Database:** PostgreSQL với tiện ích mở rộng **pgvector** (lưu trữ và tìm kiếm vector embedding)
- **AI Engine:** Google Gemini API (`gemini-2.0-flash-lite` & `gemini-embedding-001`)
- **Real-time Sync:** ASP.NET Core SignalR (Đồng bộ dữ liệu thời gian thực không cần tải lại trang)
- **Tương tác Frontend:** HTML, CSS (Custom Design), JavaScript, Bootstrap
- **Xử lý tài liệu:** Trích xuất nội dung từ định dạng `.pdf`, `.docx`, `.pptx` (Sử dụng PdfPig và OpenXML).

---

## 🏛 Kiến Trúc Hệ Thống

Dự án được xây dựng theo mô hình **3-Tier Architecture** (3 lớp) kết hợp với các dịch vụ AI và Cơ sở dữ liệu:
- **PresentationLayer:** Giao diện Razor Pages tương tác người dùng, Controllers xử lý Webhook và SignalR Hub.
- **BussinessLayer:** Xử lý các logic nghiệp vụ (Auth, Chat AI, Document Processing, Quota, Payment Gateways).
- **DataAccessLayer:** Tương tác với PostgreSQL DB thông qua Entity Framework Core.

---

## ⚙️ Hướng Dẫn Cài Đặt Chi Tiết (Dành cho Developer)

Thực hiện theo các bước dưới đây để thiết lập dự án từ đầu:

### Bước 1: Yêu cầu Hệ Thống
Trước khi bắt đầu, hãy cài đặt các công cụ sau:
1. **.NET 8 SDK:** [Tải và cài đặt .NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. **PostgreSQL:** [Tải và cài đặt PostgreSQL](https://www.postgresql.org/download/).
3. **pgvector (Bắt buộc):**
   - Tiện ích mở rộng này cần thiết để lưu trữ dữ liệu vector từ AI.
   - Trên Windows: Bạn có thể cài đặt thông qua trình quản lý Stack Builder đi kèm PostgreSQL hoặc tải file zip `pgvector` và copy vào thư mục `lib` và `share` của PostgreSQL.
   - Hoặc chạy PostgreSQL qua Docker đã tích hợp sẵn pgvector:
     ```bash
     docker run --name chatedu-pg -e POSTGRES_PASSWORD=mat_khau_cua_ban -p 5432:5432 -d pgvector/pgvector:pg16
     ```

### Bước 2: Thiết Lập Môi Trường (`.env`)
Tạo một file có tên `.env` ở **thư mục gốc** của dự án (cùng cấp với thư mục `PresentationLayer`, `BussinessLayer` và `DataAccessLayer`) với cấu trúc sau:

```env
# Cấu hình CSDL PostgreSQL (Hãy đổi sang mật khẩu của bạn)
DB_CONNECTION_STRING=Host=localhost;Database=ChatEduDb;Username=postgres;Password=mat_khau_cua_ban

# Cấu hình Gemini AI API (Lấy từ Google AI Studio)
GEMINI_API_KEY=Khóa_API_Của_Bạn_Từ_Google_AI_Studio
# Tên model sử dụng (Mặc định sẽ tự động chọn gemini-2.0-flash-lite nếu trống)
GEMINI_MODEL=gemini-2.0-flash-lite

# Cấu hình SMTP Gmail (Dành cho chức năng quên mật khẩu / gửi hóa đơn tự động)
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=email_cua_ban@gmail.com
SMTP_PASS=app_password_16_ky_tu
SMTP_FROM_NAME=EduManager

# Cấu hình Thanh toán VNPay
VNPay__TmnCode=your-vnpay-tmn-code
VNPay__HashSecret=your-vnpay-hash-secret
VNPay__BaseUrl=vpcpay.html
VNPay__ReturnUrl=Payment/Callback

# Cấu hình Thanh toán PayOS
PayOS__ClientId=your-payos-client-id
PayOS__ApiKey=your-payos-api-key
PayOS__ChecksumKey=your-payos-checksum-key

# Cấu hình Thanh toán SePay
SePay__ApiKey=your-sepay-api-key
```

### Bước 3: Khởi Tạo CSDL & Chạy Migration
Hệ thống sử dụng Entity Framework Core Code-First. Hãy chạy lệnh sau ở thư mục gốc của dự án để tự động tạo Database và cấu trúc bảng:
```bash
dotnet ef database update --project DataAccessLayer --startup-project PresentationLayer
```
*(Nếu chưa cài đặt công cụ EF Core CLI, hãy cài đặt bằng lệnh: `dotnet tool install --global dotnet-ef`)*

### Bước 4: Chạy Dự Án
Di chuyển vào thư mục `PresentationLayer` và chạy lệnh khởi động dự án:
```bash
cd PresentationLayer
dotnet run
```
Hoặc để tự động phát hiện thay đổi code và tự động tải lại (Hot Reload):
```bash
dotnet watch run
```

Sau khi chạy thành công, mở trình duyệt và truy cập vào đường dẫn:
👉 **`http://localhost:54647`** (hoặc cổng được hiển thị trên màn hình console).

---

## 📖 Hướng Dẫn Sử Dụng Nhanh

### 1. Tài Khoản Thử Nghiệm
Hệ thống mặc định có các tài khoản phân vai trò để kiểm tra:
- **Admin:** Quản lý toàn bộ môn học, xem lịch sử giao dịch toàn hệ thống và duyệt thủ công các giao dịch.
- **Giảng viên (Lecturer):** Soạn chương học, tải lên tài liệu học tập (`.pdf`, `.docx`, `.pptx`), quản lý ngân hàng câu hỏi và tạo bài kiểm tra.
- **Sinh viên (Student):** Chat AI hỗ trợ giải bài tập, đọc tài liệu, làm bài kiểm tra do giảng viên giao, nâng cấp gói dịch vụ.

### 2. Sử Dụng Cổng Thanh Toán Thử Nghiệm (Sandbox)
Khi nâng cấp gói cước hoặc mua thêm lượt hỏi trong môi trường thử nghiệm:
- **Ngân hàng:** NCB
- **Số thẻ:** `9704198526191432198`
- **Tên chủ thẻ:** `NGUYEN VAN A`
- **Ngày phát hành:** `07/15`
- **Mật khẩu OTP:** `123456`

---

## 🐞 Gỡ Lỗi Thường Gặp (Troubleshooting)

- **Lỗi 404 (Model Not Found) khi chat:** 
  Do API Key mới từ Google AI Studio của bạn không hỗ trợ các model đời cũ (như `gemini-1.5-flash`). Hãy đảm bảo biến môi trường `GEMINI_MODEL` trong file `.env` đã được cấu hình thành `gemini-2.5-flash` hoặc `gemini-2.0-flash`.
- **Lỗi pgvector không hoạt động:** 
  Đảm bảo bạn đã cài đặt extension `pgvector` vào PostgreSQL. Bạn có thể kết nối vào Database bằng pgAdmin và chạy lệnh `CREATE EXTENSION IF NOT EXISTS vector;` để kiểm tra.
- **Email không gửi được:** 
  Đảm bảo `SMTP_PASS` trong `.env` là **Mật khẩu ứng dụng (App Password)** gồm 16 ký tự được tạo từ trang quản lý tài khoản Google, chứ không phải mật khẩu đăng nhập Gmail thông thường.

---
*Phát triển và bảo trì bởi nhóm dự án ChatEdu AI - 2026*