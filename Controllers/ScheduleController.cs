using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Yoklama.Data;
using Yoklama.Models.Entities;
using Yoklama.Models.ViewModels;

namespace Yoklama.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly AppDbContext _context;

        public ScheduleController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string dayOfWeek, string groupId, string lessonTitle, string sortBy = "day")
        {
            ViewData["Title"] = "Ders Programı";
            
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return RedirectToAction("Login", "Account");

            var currentUser = await _context.Users.FindAsync(userId);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            // String parametreleri parse et
            int? parsedDayOfWeek = null;
            if (!string.IsNullOrEmpty(dayOfWeek) && int.TryParse(dayOfWeek, out var day))
            {
                parsedDayOfWeek = day;
            }

            Guid? parsedGroupId = null;
            if (!string.IsNullOrEmpty(groupId) && Guid.TryParse(groupId, out var group))
            {
                parsedGroupId = group;
            }


            var lessonsQuery = _context.Lessons
                .Where(l => l.IsActive)
                .Include(l => l.Group)
                .Include(l => l.Teacher)
                .AsQueryable();

            // Filtreleme
            if (currentUser.Role != UserRole.Admin)
            {
                // Öğretmen: sadece kendi dersleri
                lessonsQuery = lessonsQuery.Where(l => l.TeacherId == currentUser.Id);
            }
            else
            {
                // Admin: Filtreleme yapılmadıysa hiçbir ders gösterme
                var hasFilters = parsedDayOfWeek.HasValue || parsedGroupId.HasValue || !string.IsNullOrWhiteSpace(lessonTitle);
                if (!hasFilters)
                {
                    // Admin kullanıcı için filtre yoksa boş liste döndür
                    lessonsQuery = lessonsQuery.Where(l => false); // Hiçbir ders döndürmez
                }
            }

            if (parsedDayOfWeek.HasValue)
                lessonsQuery = lessonsQuery.Where(l => l.DayOfWeek == parsedDayOfWeek.Value);

            if (parsedGroupId.HasValue)
                lessonsQuery = lessonsQuery.Where(l => l.GroupId == parsedGroupId.Value);

            if (!string.IsNullOrWhiteSpace(lessonTitle))
            {
                lessonsQuery = lessonsQuery.Where(l => l.Title.ToLower().Contains(lessonTitle.ToLower()));
            }

            // Sıralama
            switch (sortBy)
            {
                case "time":
                    lessonsQuery = lessonsQuery.OrderBy(l => l.StartTime);
                    break;
                case "title":
                    lessonsQuery = lessonsQuery.OrderBy(l => l.Title);
                    break;
                default:
                    lessonsQuery = lessonsQuery.OrderBy(l => l.DayOfWeek).ThenBy(l => l.StartTime);
                    break;
            }

            var lessons = await lessonsQuery.ToListAsync();
            

            // Her ders için bugünkü oturum durumunu bul
            var today = DateTime.Today;
            var sessionIds = new Dictionary<Guid, Guid?>();
            var sessionStatuses = new Dictionary<Guid, SessionStatus?>();
            
            // Önce tüm oturumları çek, sonra client-side filtrele
            var allSessions = await _context.AttendanceSessions
                .ToListAsync();
            
            foreach (var lesson in lessons)
            {
                var session = allSessions
                    .Where(s => s.LessonId == lesson.Id)
                    .FirstOrDefault();
                
                sessionIds[lesson.Id] = session?.Id;
                sessionStatuses[lesson.Id] = session?.Status;
            }

            ViewBag.SessionIds = sessionIds;
            ViewBag.SessionStatuses = sessionStatuses;

            // Grup listesi
            var groups = currentUser.Role == UserRole.Admin 
                ? await _context.Groups.OrderBy(g => g.Name).ToListAsync() 
                : await _context.Lessons
                    .Where(l => l.TeacherId == currentUser.Id)
                    .Include(l => l.Group)
                    .Select(l => l.Group)
                    .Distinct()
                    .OrderBy(g => g.Name)
                    .ToListAsync();

            // Öğretmen listesi (admin için)
            var teachers = new List<Yoklama.Models.Entities.User>();
            if (currentUser.Role == UserRole.Admin)
            {
                teachers = await _context.Users
                    .Where(u => (u.Role == UserRole.Teacher || u.Role == UserRole.Admin) && u.IsActive)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }

            ViewBag.DayOfWeek = parsedDayOfWeek;
            ViewBag.SortBy = sortBy;
            ViewBag.SelectedLessonTitle = lessonTitle;

            // Haftalık ders programı için günleri oluştur
            var days = new List<ScheduleDayVm>();
            var dayNames = new[] { "", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi", "Pazar" };
            
            for (int i = 1; i <= 7; i++)
            {
                var dayLessons = lessons.Where(l => l.DayOfWeek == i).ToList();
                var scheduleLessons = dayLessons.Select(l => new ScheduleLessonVm
                {
                    LessonId = l.Id,
                    Title = l.Title,
                    StartTime = l.StartTime,
                    EndTime = l.EndTime,
                    GroupName = l.Group?.Name ?? "Grup Yok",
                    TeacherName = l.Teacher?.FullName ?? "Öğretmen Yok",
                    TeacherId = l.TeacherId
                }).ToList();

                days.Add(new ScheduleDayVm
                {
                    DayOfWeek = i,
                    DayName = dayNames[i],
                    Lessons = scheduleLessons
                });
            }

            var vm = new ScheduleVm
            {
                Days = days,
                Groups = groups,
                SelectedGroupId = parsedGroupId,
                IsAdmin = currentUser.Role == UserRole.Admin,
                HasAnyFilters = parsedDayOfWeek.HasValue || parsedGroupId.HasValue || !string.IsNullOrWhiteSpace(lessonTitle)
            };

            return View(vm);
        }
    }
}