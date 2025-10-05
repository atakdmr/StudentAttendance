using Microsoft.EntityFrameworkCore;
using Yoklama.Data;
using Yoklama.Models.Entities;

namespace Yoklama.Services
{
    public class LessonConflictService : ILessonConflictService
    {
        private readonly AppDbContext _db;

        public LessonConflictService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<LessonConflictResult> CheckTeacherConflictAsync(Guid teacherId, int dayOfWeek, TimeSpan startTime, TimeSpan endTime, Guid? excludeLessonId = null)
        {
            var query = _db.Lessons
                .Include(l => l.Group)
                .Where(l => l.TeacherId == teacherId && 
                           l.DayOfWeek == dayOfWeek && 
                           l.IsActive);

            if (excludeLessonId.HasValue)
            {
                query = query.Where(l => l.Id != excludeLessonId.Value);
            }

            var conflictingLessons = await query.ToListAsync();
            var conflict = conflictingLessons.FirstOrDefault(l => 
                (startTime < l.EndTime && endTime > l.StartTime));

            if (conflict != null)
            {
                return new LessonConflictResult
                {
                    HasConflict = true,
                    ConflictType = "Teacher",
                    ConflictingLessonTitle = conflict.Title,
                    ConflictingGroupName = conflict.Group.Name,
                    ConflictingStartTime = conflict.StartTime,
                    ConflictingEndTime = conflict.EndTime,
                    Message = $"Bu öğretmenin aynı günde {conflict.StartTime:hh\\:mm}-{conflict.EndTime:hh\\:mm} saatleri arasında '{conflict.Title}' dersi ({conflict.Group.Name}) bulunmaktadır."
                };
            }

            return new LessonConflictResult { HasConflict = false };
        }

        public async Task<LessonConflictResult> CheckGroupConflictAsync(Guid groupId, int dayOfWeek, TimeSpan startTime, TimeSpan endTime, Guid? excludeLessonId = null)
        {
            var query = _db.Lessons
                .Include(l => l.Teacher)
                .Where(l => l.GroupId == groupId && 
                           l.DayOfWeek == dayOfWeek && 
                           l.IsActive);

            if (excludeLessonId.HasValue)
            {
                query = query.Where(l => l.Id != excludeLessonId.Value);
            }

            var conflictingLessons = await query.ToListAsync();
            var conflict = conflictingLessons.FirstOrDefault(l => 
                (startTime < l.EndTime && endTime > l.StartTime));

            if (conflict != null)
            {
                return new LessonConflictResult
                {
                    HasConflict = true,
                    ConflictType = "Group",
                    ConflictingLessonTitle = conflict.Title,
                    ConflictingTeacherName = conflict.Teacher.FullName,
                    ConflictingStartTime = conflict.StartTime,
                    ConflictingEndTime = conflict.EndTime,
                    Message = $"Bu grubun aynı günde {conflict.StartTime:hh\\:mm}-{conflict.EndTime:hh\\:mm} saatleri arasında '{conflict.Title}' dersi ({conflict.Teacher.FullName}) bulunmaktadır."
                };
            }

            return new LessonConflictResult { HasConflict = false };
        }

        public async Task<LessonConflictResult> CheckConflictAsync(Guid teacherId, Guid groupId, int dayOfWeek, TimeSpan startTime, TimeSpan endTime, Guid? excludeLessonId = null)
        {
            // Önce öğretmen çakışmasını kontrol et
            var teacherConflict = await CheckTeacherConflictAsync(teacherId, dayOfWeek, startTime, endTime, excludeLessonId);
            if (teacherConflict.HasConflict)
            {
                return teacherConflict;
            }

            // Sonra grup çakışmasını kontrol et
            var groupConflict = await CheckGroupConflictAsync(groupId, dayOfWeek, startTime, endTime, excludeLessonId);
            if (groupConflict.HasConflict)
            {
                return groupConflict;
            }

            return new LessonConflictResult { HasConflict = false };
        }
    }
}
