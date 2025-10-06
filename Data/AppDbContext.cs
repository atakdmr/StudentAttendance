using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Yoklama.Models.Entities;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Yoklama.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Group> Groups => Set<Group>();
        public DbSet<Lesson> Lessons => Set<Lesson>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
        public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
        public DbSet<Announcement> Announcements => Set<Announcement>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // MySQL supports TimeSpan natively; no conversion required

            // User
            modelBuilder.Entity<User>(b =>
            {
                // MySQL InnoDB + utf8mb4 has 191 char safe index length (191*4 < 767 bytes)
                b.Property(u => u.UserName)
                    .HasMaxLength(191);
                b.HasIndex(u => u.UserName).IsUnique();
                b.Property(u => u.RowVersion).IsRowVersion();
            });

            // Group
            modelBuilder.Entity<Group>(b =>
            {
                b.Property(g => g.Name).IsRequired();
                b.Property(g => g.Code).IsRequired();

                b.HasMany(g => g.Students)
                    .WithOne(s => s.Group)
                    .HasForeignKey(s => s.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasMany(g => g.Lessons)
                    .WithOne(l => l.Group)
                    .HasForeignKey(l => l.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Student
            modelBuilder.Entity<Student>(b =>
            {
                b.HasIndex(s => s.StudentNumber).IsUnique();
                b.Property(s => s.StudentNumber)
                    .HasMaxLength(191);
                b.Property(s => s.FirstName).IsRequired();
                b.Property(s => s.LastName).IsRequired();
                b.Property(s => s.StudentNumber).IsRequired();
            });

            // Lesson
            modelBuilder.Entity<Lesson>(b =>
            {
                b.Property(l => l.Title).IsRequired();
                b.Property(l => l.DayOfWeek).IsRequired();

                b.HasOne(l => l.Teacher)
                    .WithMany(u => u.LessonsTaught)
                    .HasForeignKey(l => l.TeacherId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasMany(l => l.Sessions)
                    .WithOne(s => s.Lesson)
                    .HasForeignKey(s => s.LessonId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AttendanceSession
            modelBuilder.Entity<AttendanceSession>(b =>
            {
                b.Property(s => s.ScheduledAt).IsRequired();

                b.HasOne(s => s.Group)
                    .WithMany() // no direct collection on Group for sessions
                    .HasForeignKey(s => s.GroupId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(s => s.Teacher)
                    .WithMany(t => t.SessionsLed)
                    .HasForeignKey(s => s.TeacherId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasMany(s => s.Records)
                    .WithOne(r => r.Session)
                    .HasForeignKey(r => r.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AttendanceRecord
            modelBuilder.Entity<AttendanceRecord>(b =>
            {
                b.HasIndex(r => new { r.SessionId, r.StudentId }).IsUnique();
                b.Property(r => r.RowVersion).IsRowVersion();

                b.HasOne(r => r.Student)
                    .WithMany(s => s.AttendanceRecords)
                    .HasForeignKey(r => r.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
        }
        
    }
}
