using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<QuestionBank> QuestionBanks { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Seed 1 vài dữ liệu demo
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "student", PasswordHash = "student123", Role = "Student" },
                new User { Id = 2, Username = "lecturer", PasswordHash = "lecturer123", Role = "Lecturer" },
                new User { Id = 3, Username = "admin", PasswordHash = "admin123", Role = "Admin" }
            );

            // Seed dữ liệu gói mặc định
            modelBuilder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan { Id = 1, Name = "Free",    Description = "Gói miễn phí cơ bản",            Price = 0,      MonthlyQuestionLimit = 5,   IsActive = true, SortOrder = 1 },
                new SubscriptionPlan { Id = 2, Name = "Basic",   Description = "Gói cơ bản 100 câu hỏi/tháng",  Price = 50000,  MonthlyQuestionLimit = 100,  IsActive = true, SortOrder = 2 },
                new SubscriptionPlan { Id = 3, Name = "Premium", Description = "Gói cao cấp không giới hạn",    Price = 150000, MonthlyQuestionLimit = -1,   IsActive = true, SortOrder = 3 }
            );

            // Seed môn học
            modelBuilder.Entity<Subject>().HasData(
                new Subject { Id = 1, Name = "Lập trình C# (.NET)", Code = "PRN211" },
                new Subject { Id = 2, Name = "Lập trình Web với ASP.NET Core", Code = "PRN221" },
                new Subject { Id = 3, Name = "Lập trình di động MAUI", Code = "PRN231" }
            );

            // Cấu hình các quan hệ khóa ngoại để tránh vòng lặp Cascade
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
        }
    }
}
