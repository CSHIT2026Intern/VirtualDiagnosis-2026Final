using Microsoft.EntityFrameworkCore;

namespace VirtualPatDiagnosis.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
           : base(options)
        {
        }

        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<ExamCase> ExamCases { get; set; }
        public DbSet<PatientProfile> PatientProfiles { get; set; }
        public DbSet<ChecklistItem> ChecklistItems { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }
        public DbSet<StudentResponse> StudentResponses { get; set; }
        public DbSet<ChecklistScore> ChecklistScores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserAccount>().ToTable("UserAccount");
            modelBuilder.Entity<ExamCase>().ToTable("ExamCase");
            modelBuilder.Entity<PatientProfile>().ToTable("PatientProfile");
            modelBuilder.Entity<ChecklistItem>().ToTable("ChecklistItem");
            modelBuilder.Entity<ExamResult>().ToTable("ExamResults");
            modelBuilder.Entity<StudentResponse>().ToTable("StudentResponse");
            modelBuilder.Entity<ChecklistScore>().ToTable("ChecklistScore");
        }
    }
}
