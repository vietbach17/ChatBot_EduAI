using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer
{
    /// <summary>
    /// DbContext chinh cua toan bo he thong, ke thua tu Entity Framework Core. Dinh nghia tat ca cac bang (DbSet), cau hinh quan he khoa ngoai va du lieu moi (Seed Data) ban dau.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<DocumentChunk> DocumentChunks { get; set; }
        public DbSet<AddonPackage> AddonPackages { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<DocumentActivityLog> DocumentActivityLogs { get; set; }
        public DbSet<QuizActivityLog> QuizActivityLogs { get; set; }
        public DbSet<QuestionBankActivityLog> QuestionBankActivityLogs { get; set; }
        public DbSet<QuestionBank> QuestionBanks { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<AIGenerationLog> AIGenerationLogs { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        public DbSet<QuizAnswer> QuizAnswers { get; set; }
        public DbSet<Report> Reports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<DocumentChunk>()
                .HasIndex(c => c.Embedding)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops");

            base.OnModelCreating(modelBuilder);
            
            // Global Query Filters cho Soft Delete
            modelBuilder.Entity<Subject>().HasQueryFilter(s => !s.IsDeleted);
            modelBuilder.Entity<Chapter>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Document>().HasQueryFilter(d => !d.IsDeleted);
            modelBuilder.Entity<Quiz>().HasQueryFilter(q => !q.IsDeleted);
            
            // Seed 1 vài dữ liệu demo
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "student", PasswordHash = "student123", Role = "Student" },
                new User { Id = 2, Username = "lecturer", PasswordHash = "lecturer123", Role = "Lecturer" },
                new User { Id = 3, Username = "admin", PasswordHash = "admin123", Role = "Admin" }
            );

            modelBuilder.Entity<AddonPackage>().HasData(
                new AddonPackage
                {
                    Id = 1,
                    Name = "Gói Mini (Cấp tốc)",
                    Price = 10000,
                    QuotaAmount = 15,
                    IsActive = true
                },
                new AddonPackage
                {
                    Id = 2,
                    Name = "Gói Standard (Cứu cánh)",
                    Price = 20000,
                    QuotaAmount = 40,
                    IsActive = true
                },
                new AddonPackage
                {
                    Id = 3,
                    Name = "Gói Ultra (Chạy nước rút)",
                    Price = 50000,
                    QuotaAmount = 120,
                    IsActive = true
                }
            );

            modelBuilder.Entity<Subject>()
                .HasOne(s => s.Lecturer)
                .WithMany(u => u.AssignedSubjects)
                .HasForeignKey(s => s.LecturerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Subject>().HasData(
                new Subject { Id = 1, Code = "PRN222", Name = "C# Nâng cao", LecturerId = 2 },
                new Subject { Id = 2, Code = "AI101", Name = "Nhập môn AI", LecturerId = 2 }
            );

            modelBuilder.Entity<Chapter>().HasData(
                new Chapter { Id = 1, SubjectId = 1, Title = "Chương 1: .NET Core và C# Nâng cao", OrderIndex = 1 },
                new Chapter { Id = 2, SubjectId = 1, Title = "Chương 2: Entity Framework Core", OrderIndex = 2 },
                new Chapter { Id = 3, SubjectId = 2, Title = "Chương 1: Tổng quan AI", OrderIndex = 1 }
            );

            // Cấu hình liên kết của PaymentTransaction
            modelBuilder.Entity<PaymentTransaction>()
                .HasOne(t => t.User)
                .WithMany(u => u.PaymentTransactions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaymentTransaction>()
                .HasOne(t => t.SubscriptionPlan)
                .WithMany()
                .HasForeignKey(t => t.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed Data 3 gói cước mới thay thế gói cũ
            modelBuilder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan { Id = 1, Name = "Basic", Description = "Gói cơ bản miễn phí", Price = 0, MonthlyQuestionLimit = 5, IsActive = true, SortOrder = 1, DurationDays = 30, Features = "[\"Hỏi đáp AI cơ bản\", \"Độ trễ phản hồi bình thường\", \"Giới hạn 5 câu hỏi / 5 giờ\"]" },
                new SubscriptionPlan { Id = 2, Name = "Pro", Description = "Gói nâng cao nhiều tính năng", Price = 25000, MonthlyQuestionLimit = 20, IsActive = true, SortOrder = 2, DurationDays = 30, Features = "[\"Ưu tiên xử lý câu hỏi\", \"Tốc độ phản hồi AI nhanh hơn\", \"Giới hạn 20 câu hỏi / 5 giờ\", \"Hỗ trợ tài liệu đính kèm\"]" },
                new SubscriptionPlan { Id = 3, Name = "Ultra", Description = "Gói cao cấp không giới hạn", Price = 100000, MonthlyQuestionLimit = -1, IsActive = true, SortOrder = 3, DurationDays = 30, Features = "[\"Không giới hạn số câu hỏi\", \"AI phản hồi tức thì\", \"Mô hình AI cao cấp nhất\", \"Hỗ trợ ưu tiên 24/7\"]" }
            );

            // Cấu hình các quan hệ khóa ngoại để tránh vòng lặp Cascade cho QuestionBank, Quiz, QuizQuestion
            modelBuilder.Entity<QuestionBank>()
                .HasOne(q => q.Subject)
                .WithMany()
                .HasForeignKey(q => q.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuestionBank>()
                .HasOne(q => q.Lecturer)
                .WithMany()
                .HasForeignKey(q => q.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Subject)
                .WithMany()
                .HasForeignKey(q => q.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Lecturer)
                .WithMany()
                .HasForeignKey(q => q.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QuizQuestion>()
                .HasOne(qq => qq.Quiz)
                .WithMany()
                .HasForeignKey(qq => qq.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizQuestion>()
                .HasOne(qq => qq.QuestionBank)
                .WithMany()
                .HasForeignKey(qq => qq.QuestionBankId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed câu hỏi mẫu
            modelBuilder.Entity<QuestionBank>().HasData(
                new QuestionBank
                {
                    Id = 1,
                    SubjectId = 1,
                    Content = "Từ khóa nào được dùng để khai báo hằng số trong C#?",
                    QuestionType = "MultipleChoice",
                    OptionsJson = "[\"readonly\",\"const\",\"static\",\"let\"]",
                    CorrectAnswer = "B",
                    Difficulty = "Easy",
                    Tags = "Syntax,Variables",
                    IsAIGenerated = false,
                    LecturerId = 2,
                    CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
                },
                new QuestionBank
                {
                    Id = 2,
                    SubjectId = 1,
                    Content = "C# là một ngôn ngữ lập trình thuần hướng đối tượng (Pure Object-Oriented). Đúng hay Sai?",
                    QuestionType = "TrueFalse",
                    OptionsJson = null,
                    CorrectAnswer = "False",
                    Difficulty = "Easy",
                    Tags = "OOP,Theory",
                    IsAIGenerated = false,
                    LecturerId = 2,
                    CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
                },
                new QuestionBank
                {
                    Id = 3,
                    SubjectId = 2,
                    Content = "Middleware nào được sử dụng để phục vụ các tệp tĩnh (static files) như HTML, CSS, JS trong ASP.NET Core?",
                    QuestionType = "MultipleChoice",
                    OptionsJson = "[\"UseRouting()\",\"UseStaticFiles()\",\"UseEndpoints()\",\"UseHttpsRedirection()\"]",
                    CorrectAnswer = "B",
                    Difficulty = "Medium",
                    Tags = "Middleware,StaticFiles",
                    IsAIGenerated = false,
                    LecturerId = 2,
                    CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
                },
               new QuestionBank
               {
                Id = 4,
                SubjectId = 1,
                Content = "Nếu trình duyệt của client không hỗ trợ WebSockets, SignalR trong ASP.NET Core sẽ tự động fallback (lùi về) sử dụng các cơ chế nào?",
                QuestionType = "MultipleChoice",
                OptionsJson = "[\"Chỉ Long Polling\",\"Server-Sent Events và Long Polling\",\"gRPC và TCP\",\"Không có cơ chế fallback\"]",
                CorrectAnswer = "B",
                Difficulty = "Medium",
                Tags = "SignalR,WebSockets",
                IsAIGenerated = false,
                LecturerId = 2,
                CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
            },
            new QuestionBank
            {
                Id = 5,
                SubjectId = 1,
                Content = "Sự khác biệt cốt lõi giữa giao thức TCP và UDP là gì?",
                QuestionType = "MultipleChoice",
                OptionsJson = "[\"TCP nhanh hơn UDP gấp nhiều lần\",\"TCP đảm bảo truyền tin cậy (không mất gói tin), UDP thì không\",\"UDP mặc định mã hóa dữ liệu, TCP thì không\",\"Cả hai đều không yêu cầu thiết lập kết nối (connectionless)\"]",
                CorrectAnswer = "B",
                Difficulty = "Easy",
                Tags = "TCP,UDP,Protocols",
                IsAIGenerated = false,
                LecturerId = 2,
                CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
            },
            new QuestionBank
            {
                Id = 6,
                SubjectId = 1,
                Content = "Mã trạng thái HTTP (Status Code) nào báo hiệu rằng người dùng chưa đăng nhập hoặc cung cấp Token không hợp lệ?",
                QuestionType = "MultipleChoice",
                OptionsJson = "[\"400 Bad Request\",\"403 Forbidden\",\"401 Unauthorized\",\"404 Not Found\"]",
                CorrectAnswer = "C",
                Difficulty = "Easy",
                Tags = "HTTP,StatusCode,Security",
                IsAIGenerated = false,
                LecturerId = 2,
                CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
            },
            new QuestionBank
            {
                Id = 7,
                SubjectId = 1,
                Content = "CORS (Cross-Origin Resource Sharing) trong ASP.NET Core API được sử dụng để giải quyết bài toán gì?",
                QuestionType = "MultipleChoice",
                OptionsJson = "[\"Bảo vệ CSDL khỏi SQL Injection\",\"Cho phép hoặc chặn các Request gọi từ các Domain/Port khác\",\"Mã hóa mật khẩu người dùng trước khi lưu\",\"Tối ưu hóa tốc độ tải trang (Caching)\"]",
                CorrectAnswer = "B",
                Difficulty = "Medium",
                Tags = "CORS,Security,API",
                IsAIGenerated = false,
                LecturerId = 2,
                CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
            },
            new QuestionBank
            {
                Id = 8,
                SubjectId = 1,
                Content = "Trong kiến trúc gRPC của .NET, định dạng nào được sử dụng để tuần tự hóa (serialize) dữ liệu truyền đi thay cho JSON?",
                QuestionType = "MultipleChoice",
                OptionsJson = "[\"XML\",\"BSON\",\"Protocol Buffers (Protobuf)\",\"MessagePack\"]",
                CorrectAnswer = "C",
                Difficulty = "Medium",
                Tags = "gRPC,Serialization",
                IsAIGenerated = false,
                LecturerId = 2,
                CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
            },
            new QuestionBank
            {
                Id = 9,
                SubjectId = 1,
                Content = "Phương thức HTTP nào được thiết kế chuẩn để cập nhật MỘT PHẦN dữ liệu của tài nguyên thay vì cập nhật toàn bộ?",
                QuestionType = "MultipleChoice",
                OptionsJson = "[\"PUT\",\"POST\",\"PATCH\",\"UPDATE\"]",
                CorrectAnswer = "C",
                Difficulty = "Easy",
                Tags = "HTTP,REST",
                IsAIGenerated = false,
                LecturerId = 2,
                CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
            },
            new QuestionBank
            {
                Id = 10,
                SubjectId = 1,
                Content = "Để ánh xạ (map) một SignalR Hub vào Endpoint Routing trong file Program.cs của .NET 8, ta dùng phương thức nào?",
                QuestionType = "MultipleChoice",
                OptionsJson = "[\"app.MapControllers()\",\"app.AddSignalR()\",\"app.MapHub<THub>()\",\"app.UseWebSockets()\"]",
                CorrectAnswer = "C",
                Difficulty = "Medium",
                Tags = "SignalR,Routing",
                IsAIGenerated = false,
                LecturerId = 2,
                CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
            },
            new QuestionBank
            {
                Id = 11,
                SubjectId = 1,
                Content = "YARP (Yet Another Reverse Proxy) của Microsoft thường được dùng trong trường hợp nào?",
                QuestionType = "MultipleChoice",
                OptionsJson = "[\"Làm API Gateway điều hướng request cho các Microservices\",\"Thay thế Entity Framework Core\",\"Quản lý background jobs như Hangfire\",\"Tạo giao diện người dùng thời gian thực\"]",
                CorrectAnswer = "A",
                Difficulty = "Easy",
                Tags = "YARP,Microservices,Proxy",
                IsAIGenerated = false,
                LecturerId = 2,
                CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
            },
            new QuestionBank
            {
                Id = 12,
                SubjectId = 1,
                Content = "Bản chất của kết nối WebSocket là gì?",
                QuestionType = "MultipleChoice",
                OptionsJson = "[\"Giao tiếp 1 chiều từ Client lên Server\",\"Kết nối 2 chiều (Bi-directional) toàn thời gian qua 1 TCP Socket\",\"Request-Response liên tục (Polling)\",\"Chỉ mở kết nối khi Server có dữ liệu mới\"]",
                CorrectAnswer = "B",
                Difficulty = "Medium",
                Tags = "WebSockets,RealTime",
                IsAIGenerated = false,
                LecturerId = 2,
                CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
            },
            new QuestionBank
            {
                Id = 13,
                SubjectId = 1,
                Content = "Khi cấu hình Middleware trong ASP.NET Core, thứ tự khai báo (chẳng hạn UseAuthentication trước UseAuthorization) có quan trọng không?",
                QuestionType = "MultipleChoice",
                OptionsJson = "[\"Không quan trọng, .NET sẽ tự động sắp xếp lại\",\"Chỉ quan trọng khi cấu hình CORS\",\"Có, thứ tự cực kỳ quan trọng vì Request đi qua theo thứ tự khai báo\",\"Không, tất cả middleware chạy song song cùng lúc\"]",
                CorrectAnswer = "C",
                Difficulty = "Easy",
                Tags = "Middleware,ASP.NETCore",
                IsAIGenerated = false,
                LecturerId = 2,
                CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
            },
            new QuestionBank
            {
                Id = 14,
                SubjectId = 1,
                Content = "Namespace nào trong .NET cung cấp các lớp (classes) làm việc trực tiếp ở tầng thấp với TCP/UDP như TcpListener, UdpClient?",
                QuestionType = "MultipleChoice",
                OptionsJson = "[\"System.Net.Http\",\"Microsoft.AspNetCore.Http\",\"System.Net.Sockets\",\"System.IO.Ports\"]",
                CorrectAnswer = "C",
                Difficulty = "Easy",
                Tags = "Sockets,Networking",
                IsAIGenerated = false,
                LecturerId = 2,
                CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
            },
            new QuestionBank
            {
                Id = 15,
                SubjectId = 1,
                Content = "Trong SignalR, làm thế nào để Hub Server gửi một tin nhắn trực tiếp đến MỘT người dùng cụ thể đã được xác thực (đăng nhập)?",
                QuestionType = "MultipleChoice",
                OptionsJson = "[\"Clients.All.SendAsync()\",\"Clients.Others.SendAsync()\",\"Clients.Group(userId).SendAsync()\",\"Clients.User(userId).SendAsync()\"]",
                CorrectAnswer = "D",
                Difficulty = "Hard",
                Tags = "SignalR,Targeting",
                IsAIGenerated = false,
                LecturerId = 2,
                CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
            },
            new QuestionBank
            {
                Id = 16,
                SubjectId = 1,
                Content = "Công nghệ SSE (được SignalR dùng làm fallback) là viết tắt của từ gì?",
                QuestionType = "MultipleChoice",
                OptionsJson = "[\"Server-Sent Events\",\"Socket Secure Engine\",\"SignalR Streaming Extension\",\"Synchronous Server Endpoint\"]",
                CorrectAnswer = "A",
                Difficulty = "Medium",
                Tags = "SSE,RealTime",
                IsAIGenerated = false,
                LecturerId = 2,
                CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
            },
            new QuestionBank
            {
                Id = 17,
                SubjectId = 1,
                Content = "Trong ASP.NET Core, JWT (JSON Web Token) được ứng dụng chủ yếu vào mục đích gì trong môi trường mạng phân tán?",
                QuestionType = "MultipleChoice",
                OptionsJson = "[\"Lưu trữ Session trực tiếp trên RAM của Server\",\"Mã hóa dữ liệu trong Database\",\"Xác thực không trạng thái (Stateless Authentication)\",\"Chống lại tấn công DDoS\"]",
                CorrectAnswer = "C",
                Difficulty = "Medium",
                Tags = "JWT,Security,Stateless",
                IsAIGenerated = false,
                LecturerId = 2,
                CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
            },
            new QuestionBank
            {
                Id = 18,
                SubjectId = 1,
                Content = "Thuộc tính 'Idempotent' (Lũy đẳng) trong thiết kế REST API có nghĩa là gì?",
                QuestionType = "MultipleChoice",
                OptionsJson = "[\"API phản hồi cực kỳ nhanh\",\"Gọi API 1 lần hay nhiều lần liên tiếp đều sinh ra cùng một trạng thái hệ thống\",\"API chỉ cho phép giao tiếp mã hóa SSL\",\"API bắt buộc phải dùng JSON\"]",
                CorrectAnswer = "B",
                Difficulty = "Hard",
                Tags = "REST,API,Architecture",
                IsAIGenerated = false,
                LecturerId = 2,
                CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
            },
            new QuestionBank
            {
                Id = 19,
                SubjectId = 1,
                Content = "Việc thiết lập các Limits trên Kestrel Web Server (như MaxRequestBodySize, MaxConcurrentConnections) mang lại lợi ích chính gì?",
                QuestionType = "MultipleChoice",
                OptionsJson = "[\"Tránh lãng phí bộ nhớ lưu trữ CSDL\",\"Bảo vệ Server khỏi quá tải hoặc các cuộc tấn công gửi dữ liệu lớn\",\"Tự động Scale-out server lên Kubernetes\",\"Tăng tốc độ mã hóa HTTPS\"]",
                CorrectAnswer = "B",
                Difficulty = "Medium",
                Tags = "Kestrel,Security,Performance",
                IsAIGenerated = false,
                LecturerId = 2,
                CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
            },
            new QuestionBank
            {
                Id = 20,
                SubjectId = 1,
                Content = "Trong .NET, để triển khai Rate Limiting (Giới hạn số lượng request từ 1 Client) một cách native từ .NET 7/8, ta dùng tính năng nào?",
                QuestionType = "MultipleChoice",
                OptionsJson = "[\"Thư viện bên thứ ba như AspNetCoreRateLimit\",\"Rate Limiting Middleware tích hợp sẵn trong Microsoft.AspNetCore.RateLimiting\",\"Dùng Hangfire để đếm request\",\"Dùng SignalR\"]",
                CorrectAnswer = "B",
                Difficulty = "Hard",
                Tags = "RateLimiting,Middleware,Security",
                IsAIGenerated = false,
                LecturerId = 2,
                CreatedAt = new System.DateTime(2026, 7, 8, 0, 0, 0, System.DateTimeKind.Utc)
            }
            );

            modelBuilder.Entity<AIGenerationLog>()
                .HasOne(l => l.Lecturer)
                .WithMany()
                .HasForeignKey(l => l.LecturerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AIGenerationLog>()
                .HasOne(l => l.Subject)
                .WithMany()
                .HasForeignKey(l => l.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAttempt>()
                .HasOne(qa => qa.Quiz)
                .WithMany()
                .HasForeignKey(qa => qa.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAttempt>()
                .HasOne(qa => qa.Student)
                .WithMany()
                .HasForeignKey(qa => qa.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAnswer>()
                .HasOne(qa => qa.Attempt)
                .WithMany(a => a.Answers)
                .HasForeignKey(qa => qa.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAnswer>()
                .HasOne(qa => qa.QuestionBank)
                .WithMany()
                .HasForeignKey(qa => qa.QuestionBankId)
                .OnDelete(DeleteBehavior.Cascade);

            // Report: xóa người gửi thì xóa luôn báo cáo; admin xử lý dùng Restrict để tránh multiple cascade path
            modelBuilder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.HandledByAdmin)
                .WithMany()
                .HasForeignKey(r => r.HandledByAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
