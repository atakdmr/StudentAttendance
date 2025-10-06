using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Yoklama.Models.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserName { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public UserRole Role { get; set; } = UserRole.Teacher;
        public bool IsActive { get; set; } = true;
        public byte[] RowVersion { get; set; } = default!;
        // Navigation
        public ICollection<Lesson> LessonsTaught { get; set; } = new List<Lesson>();
        public ICollection<AttendanceSession> SessionsLed { get; set; } = new List<AttendanceSession>();
    }

    public class Group
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = default!;
        public string Code { get; set; } = default!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }

    public class Lesson
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid GroupId { get; set; }
        [Required(ErrorMessage = "Ders başlığı gereklidir.")]
        public string Title { get; set; } = default!;
        // ISO day of week: Monday=1 .. Sunday=7
        [Range(1, 7, ErrorMessage = "Geçerli bir gün seçin.")]
        public int DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public Guid TeacherId { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public Group Group { get; set; } = default!;
        public User Teacher { get; set; } = default!;
        public ICollection<AttendanceSession> Sessions { get; set; } = new List<AttendanceSession>();
    }

    public class Student
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string StudentNumber { get; set; } = default!;
        public string? Phone { get; set; }
        public Guid GroupId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Group Group { get; set; } = default!;
        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

        // Convenience
        public string FullName => $"{FirstName} {LastName}";
    }

    public class AttendanceSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LessonId { get; set; }
        public Guid GroupId { get; set; }
        public Guid TeacherId { get; set; }
        public DateTimeOffset ScheduledAt { get; set; }
        public DateTime? EndTime { get; set; }
        public SessionStatus Status { get; set; } = SessionStatus.Open;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // Navigation
        public Lesson Lesson { get; set; } = default!;
        public Group Group { get; set; } = default!;
        public User Teacher { get; set; } = default!;
        public ICollection<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
    }

    public class AttendanceRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SessionId { get; set; }
        public Guid StudentId { get; set; }
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
        public DateTimeOffset MarkedAt { get; set; } = DateTimeOffset.UtcNow;
        public Guid MarkedBy { get; set; }
        public string? Note { get; set; }
        public int? LateMinutes { get; set; }
        public byte[] RowVersion { get; set; } = default!;

        // Navigation
        public AttendanceSession Session { get; set; } = default!;
        public Student Student { get; set; } = default!;
    }
}
