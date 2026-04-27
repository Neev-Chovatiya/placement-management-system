using Microsoft.EntityFrameworkCore;
using pms.Models;

namespace pms.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Placement> Placements { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AdminLog> AdminLogs { get; set; }
        public DbSet<AdminSecretKey> AdminSecretKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Users -> Students (1-to-1)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Student)
                .WithOne(s => s.User)
                .HasForeignKey<Student>(s => s.UserID);

            // Companies -> Jobs (1-to-Many)
            modelBuilder.Entity<Company>()
                .HasMany(c => c.Jobs)
                .WithOne(j => j.Company)
                .HasForeignKey(j => j.CompanyID);

            // Students -> Applications (1-to-Many)
            modelBuilder.Entity<Student>()
                .HasMany(s => s.Applications)
                .WithOne(a => a.Student)
                .HasForeignKey(a => a.StudentID);

            // Jobs -> Applications (1-to-Many)
            modelBuilder.Entity<Job>()
                .HasMany(j => j.Applications)
                .WithOne(a => a.Job)
                .HasForeignKey(a => a.JobID);

            // Students -> Placements (1-to-1)
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Placement)
                .WithOne(p => p.Student)
                .HasForeignKey<Placement>(p => p.StudentID);

            // Students -> Notifications (1-to-Many)
            modelBuilder.Entity<Student>()
                .HasMany(s => s.Notifications)
                .WithOne(n => n.Student)
                .HasForeignKey(n => n.StudentID);

            // Jobs -> Placements (1-to-Many, restrict delete to avoid cascade path)
            modelBuilder.Entity<Job>()
                .HasMany(j => j.Placements)
                .WithOne(p => p.Job)
                .HasForeignKey(p => p.JobID)
                .OnDelete(DeleteBehavior.Restrict);

            // Companies -> Placements (1-to-Many, restrict delete to avoid cascade path)
            modelBuilder.Entity<Placement>()
                .HasOne(p => p.Company)
                .WithMany()
                .HasForeignKey(p => p.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            // AdminLog -> User (Admin)
            modelBuilder.Entity<AdminLog>()
                .HasOne(al => al.Admin)
                .WithMany()
                .HasForeignKey(al => al.AdminID);
        }
    }
}
