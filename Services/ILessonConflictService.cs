using Yoklama.Models.Entities;

namespace Yoklama.Services
{
    public interface ILessonConflictService
    {
        Task<LessonConflictResult> CheckTeacherConflictAsync(Guid teacherId, int dayOfWeek, TimeSpan startTime, TimeSpan endTime, Guid? excludeLessonId = null);
        Task<LessonConflictResult> CheckGroupConflictAsync(Guid groupId, int dayOfWeek, TimeSpan startTime, TimeSpan endTime, Guid? excludeLessonId = null);
        Task<LessonConflictResult> CheckConflictAsync(Guid teacherId, Guid groupId, int dayOfWeek, TimeSpan startTime, TimeSpan endTime, Guid? excludeLessonId = null);
    }

    public class LessonConflictResult
    {
        public bool HasConflict { get; set; }
        public string ConflictType { get; set; } = string.Empty;
        public string ConflictingLessonTitle { get; set; } = string.Empty;
        public string ConflictingTeacherName { get; set; } = string.Empty;
        public string ConflictingGroupName { get; set; } = string.Empty;
        public TimeSpan ConflictingStartTime { get; set; }
        public TimeSpan ConflictingEndTime { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
