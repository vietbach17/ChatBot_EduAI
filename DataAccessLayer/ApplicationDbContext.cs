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
        public DbSet<Document> Documents { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<DocumentChunk> DocumentChunks { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<DocumentActivityLog> DocumentActivityLogs { get; set; }

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
            
            // Seed 1 vài dữ liệu demo
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "student", PasswordHash = "student123", Role = "Student" },
                new User { Id = 2, Username = "lecturer", PasswordHash = "lecturer123", Role = "Lecturer" },
                new User { Id = 3, Username = "admin", PasswordHash = "admin123", Role = "Admin" }
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

            // Seed dữ liệu gói mặc định
            modelBuilder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan { Id = 1, Name = "Free",    Description = "Gói miễn phí cơ bản",            Price = 0,      MonthlyQuestionLimit = 5,   IsActive = true, SortOrder = 1 },
                new SubscriptionPlan { Id = 2, Name = "Basic",   Description = "Gói cơ bản 100 câu hỏi/tháng",  Price = 50000,  MonthlyQuestionLimit = 100,  IsActive = true, SortOrder = 2 },
                new SubscriptionPlan { Id = 3, Name = "Premium", Description = "Gói cao cấp không giới hạn",    Price = 150000, MonthlyQuestionLimit = -1,   IsActive = true, SortOrder = 3 }
            );
        }
    }
}
