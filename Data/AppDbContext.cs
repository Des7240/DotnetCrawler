using Microsoft.EntityFrameworkCore;
using DotnetCrawler.Entities;

namespace DotnetCrawler.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Subject> Subjects { get; set; } = null!;
        public DbSet<CourseThread> Threads { get; set; } = null!;
        public DbSet<Question> Questions { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<ThreadFile> ThreadFiles { get; set; } = null!;
        public DbSet<AppUser> AppUsers { get; set; } = null!;
        public DbSet<QuestionVote> QuestionVotes { get; set; } = null!;
        public DbSet<CommentVote> CommentVotes { get; set; } = null!;
        public DbSet<SystemSetting> SystemSettings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Indexes for faster querying
            modelBuilder.Entity<Subject>()
                .HasIndex(s => s.Code)
                .IsUnique();

            modelBuilder.Entity<CourseThread>()
                .HasIndex(t => new { t.SubjectId, t.Category });

            modelBuilder.Entity<Question>()
                .HasIndex(q => q.CourseThreadId);

            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<QuestionVote>()
                .HasIndex(v => new { v.UserId, v.QuestionId })
                .IsUnique();

            modelBuilder.Entity<CommentVote>()
                .HasIndex(v => new { v.UserId, v.CommentId })
                .IsUnique();
        }
    }
}
