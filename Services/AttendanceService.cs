using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Yoklama.Data;
using Yoklama.Models.Entities;
using Yoklama.Models.ViewModels;

namespace Yoklama.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly AppDbContext _db;

        public AttendanceService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<AttendanceSession> OpenOrGetSessionAsync(Guid lessonId, DateTimeOffset scheduledAt, Guid currentUserId)
        {
            // Validate lesson and authorization: teacher or admin will be enforced in controller, here ensure lesson exists
            var lesson = await _db.Lessons.AsNoTracking().FirstOrDefaultAsync(l => l.Id == lessonId && l.IsActive);
            if (lesson == null)
                throw new InvalidOperationException("Ders bulunamadı veya aktif değil.");

            // Try to find an existing session for same lesson and scheduled time (day + timeslot). We match by exact scheduledAt.
            var existing = await _db.AttendanceSessions
                .FirstOrDefaultAsync(s => s.LessonId == lessonId && s.ScheduledAt == scheduledAt);

            if (existing != null)
                return existing;

            var session = new AttendanceSession
            {
                LessonId = lesson.Id,
                GroupId = lesson.GroupId,
                TeacherId = lesson.TeacherId,
                ScheduledAt = scheduledAt,
                Status = SessionStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            await _db.AttendanceSessions.AddAsync(session);
            await _db.SaveChangesAsync();

            return session;
        }

        public async Task<AttendanceSessionVm?> GetSessionVmAsync(Guid sessionId)
        {
            var session = await _db.AttendanceSessions
                .AsNoTracking()
                .Include(s => s.Lesson)
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                return null;

            // students in group
            var students = await _db.Students
                .AsNoTracking()
                .Where(st => st.GroupId == session.GroupId && st.IsActive)
                .OrderBy(st => st.LastName).ThenBy(st => st.FirstName)
                .ToListAsync();

            // existing records for this session
            var records = await _db.AttendanceRecords
                .AsNoTracking()
                .Where(r => r.SessionId == session.Id)
                .ToListAsync();

            var vm = new AttendanceSessionVm
            {
                Session = new SessionInfoVm
                {
                    SessionId = session.Id,
                    LessonId = session.LessonId,
                    GroupId = session.GroupId,
                    TeacherId = session.TeacherId,
                    ScheduledAt = session.ScheduledAt,
                    Status = session.Status,
                    LessonTitle = session.Lesson.Title,
                    GroupName = session.Group.Name
                },
                Students = students.Select(st =>
                {
                    var rec = records.FirstOrDefault(r => r.StudentId == st.Id);
                    return new StudentAttendanceVm
                    {
                        StudentId = st.Id,
                        FullName = st.FullName,
                        StudentNumber = st.StudentNumber,
                        Status = rec?.Status ?? AttendanceStatus.Present,
                        LateMinutes = rec?.LateMinutes,
                        Note = rec?.Note,
                        RowVersion = rec?.RowVersion
                    };
                }).ToList()
            };

            return vm;
        }

        public async Task BulkMarkAsync(BulkMarkVm vm, Guid markedByUserId)
        {
            // Load session with status
            var session = await _db.AttendanceSessions.FirstOrDefaultAsync(s => s.Id == vm.SessionId);
            if (session == null) throw new InvalidOperationException("Oturum bulunamadı.");
            if (session.Status == SessionStatus.Finalized) throw new UnauthorizedAccessException("Oturum kapatılmış, yoklama alınamaz.");

            using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                foreach (var dto in vm.Students)
                {
                    await UpsertRecordAsync(session, dto, markedByUserId);
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task MarkAsync(Guid sessionId, StudentAttendanceVm dto, Guid markedByUserId)
        {
            var session = await _db.AttendanceSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null) throw new InvalidOperationException("Oturum bulunamadı.");
            if (session.Status == SessionStatus.Finalized) throw new UnauthorizedAccessException("Oturum kapatılmış, yoklama alınamaz.");

            await UpsertRecordAsync(session, dto, markedByUserId);
            await _db.SaveChangesAsync();
        }

        public async Task FinalizeSessionAsync(Guid sessionId, Guid currentUserId)
        {
            var session = await _db.AttendanceSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null) throw new InvalidOperationException("Oturum bulunamadı.");
            if (session.Status == SessionStatus.Finalized) throw new InvalidOperationException("Oturum zaten kapatılmış.");

            session.Status = SessionStatus.Finalized;
            session.EndTime = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<AttendanceSession>> GetSessionsForTeacherAsync(Guid teacherId)
        {
            return await _db.AttendanceSessions
                .AsNoTracking()
                .Include(s => s.Lesson)
                .Include(s => s.Group)
                .Where(s => s.TeacherId == teacherId)
                .OrderByDescending(s => s.Id)
                .ToListAsync();
        }

        private async Task UpsertRecordAsync(AttendanceSession session, StudentAttendanceVm dto, Guid markedByUserId)
        {
            // validate student belongs to group
            var belongs = await _db.Students.AnyAsync(st => st.Id == dto.StudentId && st.GroupId == session.GroupId);
            if (!belongs) throw new InvalidOperationException("Öğrenci bu oturumun grubuna ait değil.");

            var existing = await _db.AttendanceRecords
                .FirstOrDefaultAsync(r => r.SessionId == session.Id && r.StudentId == dto.StudentId);

            if (existing == null)
            {
                var rec = new AttendanceRecord
                {
                    SessionId = session.Id,
                    StudentId = dto.StudentId,
                    Status = dto.Status,
                    LateMinutes = dto.LateMinutes,
                    Note = dto.Note,
                    MarkedAt = DateTimeOffset.UtcNow,
                    MarkedBy = markedByUserId
                };
                await _db.AttendanceRecords.AddAsync(rec);
            }
            else
            {
                // optimistic concurrency with RowVersion
                if (dto.RowVersion != null)
                {
                    _db.Entry(existing).Property(nameof(AttendanceRecord.RowVersion)).OriginalValue = dto.RowVersion;
                }

                existing.Status = dto.Status;
                existing.LateMinutes = dto.LateMinutes;
                existing.Note = dto.Note;
                existing.MarkedAt = DateTimeOffset.UtcNow;
                existing.MarkedBy = markedByUserId;

                _db.AttendanceRecords.Update(existing);
            }
        }
    }
}
