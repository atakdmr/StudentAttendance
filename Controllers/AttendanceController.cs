using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Yoklama.Data;
using Yoklama.Services;
using Yoklama.Models.ViewModels;
using Yoklama.Models.Entities;

namespace Yoklama.Controllers
{
    [Authorize(Roles = "Teacher,Admin")]
    public class AttendanceController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IAttendanceService _attendanceService;
        private readonly IUserService _userService;

        public AttendanceController(AppDbContext db, IAttendanceService attendanceService, IUserService userService)
        {
            _db = db;
            _attendanceService = attendanceService;
            _userService = userService;
        }

        public async Task<IActionResult> Index(Guid? groupId = null)
        {
            var currentUserId = _userService.GetCurrentUserId(User);
            if (currentUserId == null) return Unauthorized();

            var isAdmin = User.IsInRole("Admin");
            IEnumerable<AttendanceSession> sessions;
            if (isAdmin)
            {
                var sessionsQuery = _db.AttendanceSessions
                    .AsNoTracking()
                    .Include(s => s.Lesson)
                    .Include(s => s.Group)
                    .AsQueryable()
                    .Where(s => s.Status != SessionStatus.Finalized);

                if (groupId.HasValue)
                {
                    sessionsQuery = sessionsQuery.Where(s => s.GroupId == groupId.Value);
                }

                sessions = await sessionsQuery
                    .OrderByDescending(s => s.Id)
                    .ToListAsync();
            }
            else
            {
                sessions = await _attendanceService.GetSessionsForTeacherAsync(currentUserId.Value);

                // Öğretmenin bu hafta için oturumu olmayan derslerini getir (Yoklama Başlat için)
                var today = DateTimeOffset.Now.Date;
                var weekStart = today.AddDays(-(int)today.DayOfWeek + 1);
                var weekEnd = weekStart.AddDays(7);

                var teacherLessons = await _db.Lessons
                    .AsNoTracking()
                    .Include(l => l.Group)
                    .Where(l => l.IsActive && l.TeacherId == currentUserId.Value)
                    .ToListAsync();

                // SQLite DateTimeOffset kısıtları nedeniyle client-side filtreleme
                var allSessions = await _db.AttendanceSessions
                    .AsNoTracking()
                    .ToListAsync();

                var lessonsToStart = teacherLessons
                    .Where(l => !allSessions.Any(s =>
                        s.LessonId == l.Id &&
                        s.ScheduledAt.Date >= weekStart &&
                        s.ScheduledAt.Date < weekEnd &&
                        s.Status != SessionStatus.Finalized))
                    .OrderBy(l => l.DayOfWeek)
                    .ThenBy(l => l.StartTime)
                    .ToList();

                ViewBag.LessonsToStart = lessonsToStart;
            }

            // Admin için grup listesi
            var groups = isAdmin ? await _db.Groups.OrderBy(g => g.Name).ToListAsync() : new List<Group>();

            ViewBag.Groups = groups;
            ViewBag.SelectedGroupId = groupId;
            ViewBag.IsAdmin = isAdmin;

            return View(sessions);
        }

        [HttpGet]
        public async Task<IActionResult> Session(Guid sessionId)
        {
            var vm = await _attendanceService.GetSessionVmAsync(sessionId);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> BulkMark(BulkMarkVm vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Form doğrulama hatası.";
                return RedirectToAction(nameof(Session), new { sessionId = vm.SessionId });
            }

            var currentUserId = _userService.GetCurrentUserId(User);
            if (currentUserId == null) return Unauthorized();

            try
            {
                await _attendanceService.BulkMarkAsync(vm, currentUserId.Value);
                TempData["Success"] = "Yoklama kaydedildi.";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Bu kayıt başka biri tarafından değiştirildi. Lütfen sayfayı yenileyin.";
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["Error"] = ex.Message;
                return Forbid();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "İşlem sırasında hata oluştu: " + ex.Message;
            }

            return RedirectToAction(nameof(Session), new { sessionId = vm.SessionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Mark(Guid sessionId, StudentAttendanceVm student)
        {
            var currentUserId = _userService.GetCurrentUserId(User);
            if (currentUserId == null) return Unauthorized();

            try
            {
                await _attendanceService.MarkAsync(sessionId, student, currentUserId.Value);
                TempData["Success"] = "Öğrenci yoklama kaydı güncellendi.";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Bu kayıt başka biri tarafından değiştirildi. Lütfen sayfayı yenileyin.";
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["Error"] = ex.Message;
                return Forbid();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "İşlem sırasında hata oluştu: " + ex.Message;
            }

            return RedirectToAction(nameof(Session), new { sessionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> CloseSession(Guid sessionId)
        {
            var currentUserId = _userService.GetCurrentUserId(User);
            if (currentUserId == null) return Unauthorized();

            try
            {
                await _attendanceService.FinalizeSessionAsync(sessionId, currentUserId.Value);
                TempData["Success"] = "Oturum kapatıldı.";
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["Error"] = ex.Message;
                return Forbid();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "İşlem sırasında hata oluştu: " + ex.Message;
            }

            return RedirectToAction(nameof(Session), new { sessionId });
        }

    }
}
