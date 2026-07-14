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
