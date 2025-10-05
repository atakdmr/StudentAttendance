using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Yoklama.Models.Entities;
using Yoklama.Models.ViewModels;

namespace Yoklama.Services
{
    public interface IAttendanceService
    {
        Task<AttendanceSession> CreateNewSessionAsync(Guid lessonId, DateTime sessionDate, Guid currentUserId);
        Task<AttendanceSession?> GetExistingSessionAsync(Guid lessonId, DateTime sessionDate);
        Task<AttendanceSession?> GetWeeklySessionAsync(Guid lessonId, DateTime sessionDate);
        Task<AttendanceSession> OpenOrGetSessionAsync(Guid lessonId, DateTimeOffset scheduledAt, Guid currentUserId); // Backward compatibility
        Task<AttendanceSessionVm?> GetSessionVmAsync(Guid sessionId);
        Task BulkMarkAsync(BulkMarkVm vm, Guid markedByUserId);
        Task MarkAsync(Guid sessionId, StudentAttendanceVm dto, Guid markedByUserId);
        Task FinalizeSessionAsync(Guid sessionId, Guid currentUserId);
        Task<IEnumerable<AttendanceSession>> GetSessionsForTeacherAsync(Guid teacherId);
        Task<AttendanceSession> UpdateSessionForNewWeekAsync(Guid sessionId, DateTime newDate, Guid currentUserId);
    }
}
