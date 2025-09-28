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
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // SQLite TimeSpan conversion to ticks (long)
            var timeSpanToTicksConverter = new ValueConverter<TimeSpan, long>(
                v => v.Ticks,
                v => TimeSpan.FromTicks(v));

            modelBuilder.Entity<Lesson>()
                .Property(x => x.StartTime)
                .HasConversion(timeSpanToTicksConverter);

            modelBuilder.Entity<Lesson>()
                .Property(x => x.EndTime)
                .HasConversion(timeSpanToTicksConverter);

            // User
            modelBuilder.Entity<User>(b =>
            {
                b.HasIndex(u => u.UserName).IsUnique();
                b.Property(u => u.RowVersion).IsRowVersion().HasDefaultValueSql("randomblob(8)");
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
                b.Property(r => r.RowVersion).IsRowVersion().HasDefaultValueSql("randomblob(8)");

                b.HasOne(r => r.Student)
                    .WithMany(s => s.AttendanceRecords)
                    .HasForeignKey(r => r.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AuditLog
            modelBuilder.Entity<AuditLog>(b =>
            {
                b.Property(a => a.Action).IsRequired();
                b.Property(a => a.Entity).IsRequired();
                b.Property(a => a.EntityId).IsRequired();
                b.Property(a => a.Timestamp).IsRequired();

                b.HasOne(a => a.User)
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private Guid? TryGetCurrentUserId()
        {
            var idStr = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idStr, out var id) ? id : (Guid?)null;
        }

        private void CaptureAuditLogs()
        {
            var userId = TryGetCurrentUserId();

            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is not AuditLog &&
                            (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
                .ToList();

            foreach (var entry in entries)
            {
                var action = entry.State == EntityState.Added ? "Create"
                    : entry.State == EntityState.Modified ? "Update"
                    : "Delete";

                var entityName = entry.Entity.GetType().Name;
                string entityId = "";

                var idProp = entry.Properties.FirstOrDefault(p => string.Equals(p.Metadata.Name, "Id", StringComparison.OrdinalIgnoreCase));
                if (idProp != null && idProp.CurrentValue != null)
                {
                    entityId = idProp.CurrentValue.ToString() ?? string.Empty;
                }

                string? details = null;
                if (entry.State == EntityState.Modified)
                {
                    var changed = new List<object>();
                    
                    foreach (var property in entry.Properties)
                    {
                        if (property.IsModified)
                        {
                            var originalValue = property.OriginalValue?.ToString() ?? "";
                            var currentValue = property.CurrentValue?.ToString() ?? "";
                            
                            // Sadece gerçekten farklı olan değerleri kaydet
                            if (originalValue != currentValue)
                            {
                                changed.Add(new { 
                                    Property = property.Metadata.Name, 
                                    Old = originalValue, 
                                    New = currentValue 
                                });
                            }
                        }
                    }
                    
                    if (changed.Count > 0)
                    {
                        details = JsonSerializer.Serialize(changed);
                    }
                }

                AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    Entity = entityName,
                    EntityId = entityId,
                    Timestamp = DateTime.UtcNow,
                    DetailsJson = details
                });
            }
        }

        public override int SaveChanges()
        {
            CaptureAuditLogs();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            CaptureAuditLogs();
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
