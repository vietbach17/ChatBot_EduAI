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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Seed 1 vÃ i dá»¯ liá»‡u demo
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "student", PasswordHash = "student123", Role = "Student" },
                new User { Id = 2, Username = "lecturer", PasswordHash = "lecturer123", Role = "Lecturer" },
                new User { Id = 3, Username = "admin", PasswordHash = "admin123", Role = "Admin" }
            );

            // Seed dá»¯ liá»‡u gÃ³i máº·c Ä‘á»‹nh
            modelBuilder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan { Id = 1, Name = "Free",    Description = "GÃ³i miá»…n phÃ­ cÆ¡ báº£n",            Price = 0,      MonthlyQuestionLimit = 5,   IsActive = true, SortOrder = 1 },
                new SubscriptionPlan { Id = 2, Name = "Basic",   Description = "GÃ³i cÆ¡ báº£n 100 cÃ¢u há»i/thÃ¡ng",  Price = 50000,  MonthlyQuestionLimit = 100,  IsActive = true, SortOrder = 2 },
                new SubscriptionPlan { Id = 3, Name = "Premium", Description = "GÃ³i cao cáº¥p khÃ´ng giá»›i háº¡n",    Price = 150000, MonthlyQuestionLimit = -1,   IsActive = true, SortOrder = 3 }
            );
        }
    }
}
