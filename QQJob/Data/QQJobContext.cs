using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QQJob.Models;

namespace QQJob.Data
{
    public class QQJobContext:IdentityDbContext<AppUser>
    {
        public DbSet<Employer> Employers { get; set; }
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<Education> Educations { get; set; }
        public DbSet<Award> Awards { get; set; }
        public DbSet<CandidateExp> CandidateExps { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<SavedJob> SavedJobs { get; set; }
        public DbSet<ViewJobHistory> ViewJobHistories { get; set; }
        public DbSet<Notification> NotificationHistories { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<CompanyEvident> CompanyEvidents { get; set; }
        public DbSet<JobEmbedding> JobEmbeddings { get; set; }
        public DbSet<JobSimilarityMatrix> JobSimilarityMatrix { get; set; }
        public QQJobContext(DbContextOptions<QQJobContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Đổi tên UserId thành EmployerId trong bảng Employer
            modelBuilder.Entity<Employer>()
                .HasKey(e => e.EmployerId); // Sử dụng EmployerId làm khóa chính
            modelBuilder.Entity<Employer>()
                .HasOne(e => e.User)
                .WithOne(u => u.Employer)
                .HasForeignKey<Employer>(e => e.EmployerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Đổi tên UserId thành CandidateId trong bảng Candidate
            modelBuilder.Entity<Candidate>()
                .HasKey(c => c.CandidateId); // Sử dụng CandidateId làm khóa chính
            modelBuilder.Entity<Candidate>()
                .HasOne(c => c.User)
                .WithOne(u => u.Candidate)
                .HasForeignKey<Candidate>(c => c.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);

            // Candidate - Skill N:N
            modelBuilder.Entity<Candidate>()
                .HasMany(c => c.Skills)
                .WithMany(s => s.Candidates)
                .UsingEntity(j => j.ToTable("CandidateSkills"));

            // Job - Skill N:N
            modelBuilder.Entity<Job>()
                .HasMany(j => j.Skills)
                .WithMany(s => s.Jobs)
                .UsingEntity(j => j.ToTable("JobSkills"));

            // Job - Employer 1:N
            modelBuilder.Entity<Job>()
                .HasOne(j => j.Employer)
                .WithMany(e => e.Jobs)
                .HasForeignKey(j => j.EmployerId)
                .OnDelete(DeleteBehavior.Cascade);

            //SevedJob
            modelBuilder.Entity<SavedJob>()
                .HasKey(e => e.SaveJobId);

            //Follow
            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Candidate)
                .WithMany(c => c.Follows)
                .HasForeignKey(f => f.CandidateId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Employer)
                .WithMany(c => c.Follows)
                .HasForeignKey(f => f.EmployerId)
                .OnDelete(DeleteBehavior.NoAction);

            //Application
            modelBuilder.Entity<Application>()
                .HasOne(a => a.Job)
                .WithMany(j => j.Applications)
                .HasForeignKey(a => a.JobId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SavedJob>()
                .HasOne(s => s.Job)
                .WithMany(j => j.SavedJobs)
                .HasForeignKey(s => s.JobId)
                .OnDelete(DeleteBehavior.NoAction);

            // Các ràng buộc bổ sung
            modelBuilder.Entity<Job>()
                .Property(j => j.JobTitle)
                .IsRequired()
                .HasMaxLength(255);

            //Application
            modelBuilder.Entity<Application>()
                .HasOne(a => a.Job)
                .WithMany(j => j.Applications)
                .HasForeignKey(a => a.JobId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SavedJob>()
                .HasOne(s => s.Job)
                .WithMany(j => j.SavedJobs)
                .HasForeignKey(s => s.JobId)
                .OnDelete(DeleteBehavior.NoAction);

            // ChatSession primary key
            modelBuilder.Entity<ChatSession>()
                .HasKey(cs => cs.ChatId);

            // User1 relationship (no inverse navigation)
            modelBuilder.Entity<ChatSession>()
                .HasOne(cs => cs.User1)
                .WithMany()
                .HasForeignKey(cs => cs.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            // User2 relationship (no inverse navigation)
            modelBuilder.Entity<ChatSession>()
                .HasOne(cs => cs.User2)
                .WithMany()
                .HasForeignKey(cs => cs.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            // ChatMessage primary key
            modelBuilder.Entity<ChatMessage>()
                .HasKey(cm => cm.MessageId);

            // ChatMessage → ChatSession relationship
            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.ChatSession)
                .WithMany(cs => cs.Messages)
                .HasForeignKey(cm => cm.ChatId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade path issue

            // Index for performance on ChatId + SentAt
            modelBuilder.Entity<ChatMessage>()
                .HasIndex(cm => new { cm.ChatId,cm.SentAt });

            modelBuilder.Entity<JobEmbedding>(entity =>
            {
                entity.HasKey(e => e.JobId);
                entity.Property(e => e.JobId).ValueGeneratedNever();  // IMPORTANT: no auto-increment
            });
        }
    }
}
