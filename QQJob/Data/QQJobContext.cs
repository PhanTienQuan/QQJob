using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QQJob.Models;

namespace QQJob.Data
{
    public class QQJobContext : IdentityDbContext<AppUser>
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

        public QQJobContext ( DbContextOptions<QQJobContext> options )
            : base(options)
        {
        }

        protected override void OnConfiguring ( DbContextOptionsBuilder optionsBuilder )
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating ( ModelBuilder modelBuilder )
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
                .OnDelete(DeleteBehavior.Restrict);

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


            // Các ràng buộc bổ sung
            modelBuilder.Entity<Job>()
                .Property(j => j.Title)
                .IsRequired()
                .HasMaxLength(255);
        }
    }
}
