using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Yoklama.Data;
using Yoklama.Models.Entities;
using Yoklama.Services.Sms;

namespace Yoklama.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AbsencesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ISmsService _smsService;

        public AbsencesController(AppDbContext context, ISmsService smsService)
        {
            _context = context;
            _smsService = smsService;
        }

        // Lists students who are Absent in finalized sessions
        public async Task<IActionResult> Index(string search, Guid? groupId, DateTime? startDate, DateTime? endDate, string sortBy = "date")
        {
            var query = _context.AttendanceRecords
                .Where(r => r.Status == AttendanceStatus.Absent && r.Session.Status == SessionStatus.Finalized)
                .Include(r => r.Student)
                .Include(r => r.Session)
                    .ThenInclude(s => s.Lesson)
                        .ThenInclude(l => l.Group)
                .AsNoTracking();

            // Group filter (server-side)
            if (groupId.HasValue)
            {
                query = query.Where(r => r.Session.Lesson.GroupId == groupId.Value);
            }

            var absents = await query.ToListAsync();

            // Date filters (client-side)
            if (startDate.HasValue)
            {
                absents = absents.Where(r => r.Session.ScheduledAt.Date >= startDate.Value.Date).ToList();
            }
            if (endDate.HasValue)
            {
                absents = absents.Where(r => r.Session.ScheduledAt.Date <= endDate.Value.Date).ToList();
            }

            // Search filter (client-side)
            if (!string.IsNullOrEmpty(search))
            {
                absents = absents.Where(r => r.Student.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                            r.Student.StudentNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                           r.Session.Lesson.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                           r.Session.Lesson.Group.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                                 .ToList();
            }

            // Client-side sorting
            absents = sortBy switch
            {
                "student" => absents.OrderBy(r => r.Student.FullName).ToList(),
                "group" => absents.OrderBy(r => r.Session.Lesson.Group.Name).ThenBy(r => r.Student.FullName).ToList(),
                "lesson" => absents.OrderBy(r => r.Session.Lesson.Title).ThenBy(r => r.Student.FullName).ToList(),
                _ => absents.OrderByDescending(r => r.Session.ScheduledAt).ToList()
            };

            // Get groups for filter dropdown
            var groups = await _context.Groups
                .Where(g => g.Students.Any())
                .OrderBy(g => g.Name)
                .Select(g => new { g.Id, g.Name })
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.GroupId = groupId;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.SortBy = sortBy;
            ViewBag.Groups = groups;

            return View(absents);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendSms()
        {
            var absents = await _context.AttendanceRecords
                .Where(r => r.Status == AttendanceStatus.Absent && r.Session.Status == SessionStatus.Finalized)
                .Include(r => r.Student)
                .Include(r => r.Session)
                    .ThenInclude(s => s.Lesson)
                        .ThenInclude(l => l.Group)
                .AsNoTracking()
                .ToListAsync();

            var messages = absents
                .Where(a => !string.IsNullOrWhiteSpace(a.Student.Phone))
                .GroupBy(a => a.Student.Phone!)
                .Select(g => (
                    phone: g.Key!,
                    message: $"Sayın veli, öğrenciniz bazı ders(ler)e katılmamıştır. Son yoklama: {g.Max(x => x.Session.ScheduledAt):dd.MM.yyyy HH:mm}."
                ));

            await _smsService.SendBulkAsync(messages);
            TempData["Success"] = "SMS gönderimi başlatıldı.";
            return RedirectToAction(nameof(Index));
        }
    }
}


