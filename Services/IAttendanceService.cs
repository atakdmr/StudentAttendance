using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Yoklama.Models.Entities;
using Yoklama.Models.ViewModels;

namespace Yoklama.Services
{
    public interface IAttendanceService
    {
        Task<AttendanceSession> OpenOrGetSessionAsync(Guid lessonId, DateTimeOffset scheduledAt, Guid currentUserId);
        Task<AttendanceSessionVm?> GetSessionVmAsync(Guid sessionId);
        Task BulkMarkAsync(BulkMarkVm vm, Guid markedByUserId);
        Task MarkAsync(Guid sessionId, StudentAttendanceVm dto, Guid markedByUserId);
        Task FinalizeSessionAsync(Guid sessionId, Guid currentUserId);
        Task<IEnumerable<AttendanceSession>> GetSessionsForTeacherAsync(Guid teacherId);
    }
}
