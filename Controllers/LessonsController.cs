using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Yoklama.Data;
using Yoklama.Models.Entities;
using Yoklama.Models.ViewModels;
using Yoklama.Services;

namespace Yoklama.Controllers
{
    [Authorize(Roles = "Teacher,Admin")]
    public class LessonsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IAttendanceService _attendanceService;
        private readonly IUserService _userService;

        public LessonsController(AppDbContext db, IAttendanceService attendanceService, IUserService userService)
        {
            _db = db;
            _attendanceService = attendanceService;
            _userService = userService;
        }

        public async Task<IActionResult> Index(Guid? groupId = null, Guid? teacherId = null, string? lessonTitle = null, int? dayOfWeek = null)
        {
            var currentUserId = _userService.GetCurrentUserId(User);
            if (currentUserId == null) return Unauthorized();

            var isAdmin = User.IsInRole(UserRole.Admin.ToString());
            var query = _db.Lessons.Where(l => l.IsActive);
            
            // Admin olmayan kullanıcılar sadece kendi derslerini görür
            if (!isAdmin) 
            {
                query = query.Where(l => l.TeacherId == currentUserId.Value);
                
                // Öğretmen için filtreleme
                if (groupId.HasValue)
                {
                    query = query.Where(l => l.GroupId == groupId.Value);
                }
                
                if (!string.IsNullOrWhiteSpace(lessonTitle))
                {
                    query = query.Where(l => l.Title.ToLower().Contains(lessonTitle.ToLower()));
                }
                
                if (dayOfWeek.HasValue)
                {
                    query = query.Where(l => l.DayOfWeek == dayOfWeek.Value);
                }
            }
            else
            {
                // Admin için filtreleme
                if (groupId.HasValue)
                {
                    query = query.Where(l => l.GroupId == groupId.Value);
                }
                
                if (teacherId.HasValue)
                {
                    query = query.Where(l => l.TeacherId == teacherId.Value);
                }
                
                if (!string.IsNullOrWhiteSpace(lessonTitle))
                {
                    query = query.Where(l => l.Title.ToLower().Contains(lessonTitle.ToLower()));
                }
                
                if (dayOfWeek.HasValue)
                {
                    query = query.Where(l => l.DayOfWeek == dayOfWeek.Value);
                }
            }

            var lessons = await query
                .Include(l => l.Group)
                .Include(l => l.Teacher)
                .OrderBy(l => l.DayOfWeek)
                .ThenBy(l => l.StartTime)
                .ToListAsync();

            // Teacher bilgilerini manuel olarak yükle (Include çalışmıyor)
            var teacherIds = lessons.Select(l => l.TeacherId).Distinct().ToList();
            var teachers = await _db.Users.Where(u => teacherIds.Contains(u.Id)).ToListAsync();
            
            // Debug: Teacher verilerini kontrol et
            Console.WriteLine($"Found {teachers.Count} teachers for {teacherIds.Count} teacher IDs");
            foreach (var teacher in teachers)
            {
                Console.WriteLine($"Teacher: {teacher.Id} - FullName: '{teacher.FullName}' (Length: {teacher.FullName?.Length ?? 0})");
            }

            // Bugünün tarihini al
            var today = DateTimeOffset.Now.Date;
            var todayDayOfWeek = (int)today.DayOfWeek;
            if (todayDayOfWeek == 0) todayDayOfWeek = 7; // Pazar = 7
            
            // Tüm oturumları çek (SQLite DateTimeOffset sorunu için tarih filtrelemesi yapmıyoruz)
            var allSessions = await _db.AttendanceSessions
                .ToListAsync();
            
            // Her ders için oturum durumunu kontrol et
            var lessonsWithSessions = new List<LessonWithSessionVm>();
            
            foreach (var lesson in lessons)
            {
                // Teacher bilgisini manuel olarak bul ve ata
                var teacher = teachers.FirstOrDefault(t => t.Id == lesson.TeacherId);
                
                var lessonVm = new LessonWithSessionVm
                {
                    Id = lesson.Id,
                    Title = lesson.Title,
                    DayOfWeek = lesson.DayOfWeek,
                    StartTime = lesson.StartTime,
                    EndTime = lesson.EndTime,
                    IsActive = lesson.IsActive,
                    Group = lesson.Group,
                    Teacher = teacher // Doğrudan teachers listesinden al
                };
                
                Console.WriteLine($"Lesson {lesson.Title}: TeacherId={lesson.TeacherId}, Teacher={teacher?.FullName ?? "NULL"} (Teacher object: {teacher != null})");

                // Bu ders için en son oturumu kontrol et
                var session = allSessions
                    .Where(s => s.LessonId == lesson.Id)
                    .OrderByDescending(s => s.ScheduledAt)
                    .FirstOrDefault();

                if (session != null)
                {
                    lessonVm.SessionId = session.Id;
                    lessonVm.SessionStatus = session.Status;
                }

                lessonsWithSessions.Add(lessonVm);
            }

            // Filtreleme verilerini yükle
            var groups = new List<Group>();
            var teachersList = new List<User>();
            
            if (isAdmin)
            {
                // Admin için tüm gruplar ve öğretmenler
                groups = await _db.Groups.OrderBy(g => g.Name).ToListAsync();
                teachersList = await _db.Users
                    .Where(u => (u.Role == UserRole.Teacher || u.Role == UserRole.Admin) && u.IsActive)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            else
            {
                // Öğretmen için sadece kendi derslerinin grupları
                var lessonGroupIds = lessons.Select(l => l.GroupId).Distinct().ToList();
                groups = await _db.Groups
                    .Where(g => lessonGroupIds.Contains(g.Id))
                    .OrderBy(g => g.Name)
                    .ToListAsync();
            }
            
            ViewBag.Groups = groups;
            ViewBag.Teachers = teachersList;
            ViewBag.SelectedGroupId = groupId;
            ViewBag.SelectedTeacherId = teacherId;
            ViewBag.SelectedLessonTitle = lessonTitle;
            ViewBag.SelectedDayOfWeek = dayOfWeek;
            
            // Admin için dersleri grupla ve öğretmen sayısını hesapla
            if (isAdmin)
            {
                var groupedLessons = lessonsWithSessions
                    .GroupBy(l => l.Title)
                    .Select(g => new LessonWithSessionVm
                    {
                        Id = g.First().Id, // İlk dersin ID'sini kullan (expand için)
                        Title = g.Key,
                        DayOfWeek = g.First().DayOfWeek,
                        StartTime = g.First().StartTime,
                        EndTime = g.First().EndTime,
                        IsActive = g.First().IsActive,
                        Group = g.First().Group,
                        Teacher = g.First().Teacher,
                        SessionId = g.First().SessionId,
                        SessionStatus = g.First().SessionStatus,
                        TeacherCount = g.Count(), // Öğretmen sayısını ekle
                        AllLessons = g.ToList() // Tüm dersleri sakla
                    })
                    .ToList();
                
                var vm = new LessonsVm
                {
                    Lessons = groupedLessons,
                    IsAdmin = isAdmin
                };
                
                return View(vm);
            }

            var vm2 = new LessonsVm
            {
                Lessons = lessonsWithSessions,
                IsAdmin = isAdmin
            };

            return View(vm2);
        }

        // GET: /Lessons/OpenSession?lessonId={id}&scheduledAt=yyyy-MM-ddTHH:mm (optional)
        [HttpGet]
        public async Task<IActionResult> OpenSession(Guid lessonId, DateTimeOffset? scheduledAt)
        {
            var currentUserId = _userService.GetCurrentUserId(User);
            if (currentUserId == null) return Unauthorized();

            var lesson = await _db.Lessons.AsNoTracking().FirstOrDefaultAsync(l => l.Id == lessonId && l.IsActive);
            if (lesson == null) return NotFound();

            // Authorization: Teacher can only open their own lessons; Admin can open any
            var isAdmin = User.IsInRole(UserRole.Admin.ToString());
            if (!isAdmin && lesson.TeacherId != currentUserId.Value)
            {
                return Forbid();
            }

            // Compute default scheduledAt when not provided:
            // Use today's date with the lesson's StartTime; if day of week differs, pick the next occurrence of lesson.DayOfWeek.
            var when = scheduledAt ?? ComputeNextOccurrenceWithTime(lesson.DayOfWeek, lesson.StartTime);

            var session = await _attendanceService.OpenOrGetSessionAsync(lessonId, when, currentUserId.Value);
            return RedirectToAction("Session", "Attendance", new { sessionId = session.Id });
        }

        // GET: /Lessons/CreateLesson
        [HttpGet]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> CreateLesson(Guid? groupId)
        {
            var currentUserId = _userService.GetCurrentUserId(User);
            if (currentUserId == null) return Unauthorized();

            var vm = new CreateEditVm
            {
                TeacherId = currentUserId.Value,
                IsActive = true
            };

            if (groupId.HasValue)
            {
                vm.GroupId = groupId.Value;
            }

            var groups = await _db.Groups.OrderBy(g => g.Name).ToListAsync();
            ViewBag.Groups = groups;
            if (User.IsInRole(UserRole.Admin.ToString()))
            {
                // Allow assigning lessons to both Teachers and Admins in create view
                var teachers = await _db.Users.Where(u => (u.Role == UserRole.Teacher || u.Role == UserRole.Admin) && u.IsActive)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
                ViewBag.Teachers = teachers;
            }
            else
            {
                // For non-admin users, set current user as the only teacher option
                var currentUser = await _db.Users.FindAsync(currentUserId.Value);
                if (currentUser != null)
                {
                    ViewBag.Teachers = new List<User> { currentUser };
                }
            }
            return View("Create", vm);
        }

        // POST: /Lessons/CreateLesson
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> CreateLesson(CreateEditVm vm)
        {
            var currentUserId = _userService.GetCurrentUserId(User);
            if (currentUserId == null) return Unauthorized();

            // For non-admin users, ensure TeacherId is set to current user
            if (!User.IsInRole(UserRole.Admin.ToString()))
            {
                vm.TeacherId = currentUserId.Value;
            }

            if (vm.GroupId == Guid.Empty)
            {
                ModelState.AddModelError("GroupId", "Grup seçmelisiniz.");
            }

            if (vm.TeacherId == Guid.Empty)
            {
                ModelState.AddModelError("TeacherId", "Öğretmen seçmelisiniz.");
            }

            if (string.IsNullOrWhiteSpace(vm.Title))
            {
                ModelState.AddModelError("Title", "Ders başlığı gereklidir.");
            }

            if (vm.DayOfWeek < 1 || vm.DayOfWeek > 7)
            {
                ModelState.AddModelError("DayOfWeek", "Geçerli bir gün seçin.");
            }

            if (vm.StartTime == TimeSpan.Zero)
            {
                ModelState.AddModelError("StartTime", "Başlangıç saati gereklidir.");
            }

            if (vm.EndTime == TimeSpan.Zero)
            {
                ModelState.AddModelError("EndTime", "Bitiş saati gereklidir.");
            }

            if (vm.EndTime <= vm.StartTime)
            {
                ModelState.AddModelError("EndTime", "Bitiş saati başlangıç saatinden sonra olmalıdır.");
            }

            if (vm.GroupId != Guid.Empty && !await _db.Groups.AnyAsync(g => g.Id == vm.GroupId))
            {
                ModelState.AddModelError("GroupId", "Seçilen grup bulunamadı.");
            }

            if (vm.TeacherId != Guid.Empty && !await _db.Users.AnyAsync(u => u.Id == vm.TeacherId))
            {
                ModelState.AddModelError("TeacherId", "Seçilen öğretmen bulunamadı.");
            }

            if (!ModelState.IsValid)
            {
                var groups = await _db.Groups.OrderBy(g => g.Name).ToListAsync();
                ViewBag.Groups = groups;
                return View("Create", vm);
            }

            var lesson = new Lesson
            {
                Title = vm.Title,
                DayOfWeek = vm.DayOfWeek,
                StartTime = vm.StartTime,
                EndTime = vm.EndTime,
                GroupId = vm.GroupId,
                TeacherId = vm.TeacherId,
                IsActive = vm.IsActive
            };

            _db.Lessons.Add(lesson);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Ders başarıyla eklendi.";
            return RedirectToAction("Index");
        }

        // GET: /Lessons/EditLesson/{id}
        [HttpGet]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> EditLesson(Guid id)
        {
            var currentUserId = _userService.GetCurrentUserId(User);
            if (currentUserId == null) return Unauthorized();

            var lesson = await _db.Lessons.Include(l => l.Group).FirstOrDefaultAsync(l => l.Id == id);
            if (lesson == null) return NotFound();

            var isAdmin = User.IsInRole(UserRole.Admin.ToString());
            if (!isAdmin && lesson.TeacherId != currentUserId.Value)
            {
                return Forbid();
            }

            var vm = new CreateEditVm
            {
                Id = lesson.Id,
                Title = lesson.Title,
                DayOfWeek = lesson.DayOfWeek,
                StartTime = lesson.StartTime,
                EndTime = lesson.EndTime,
                GroupId = lesson.GroupId,
                TeacherId = lesson.TeacherId,
                IsActive = lesson.IsActive
            };

            var groups = await _db.Groups.OrderBy(g => g.Name).ToListAsync();
            ViewBag.Groups = groups;

            if (User.IsInRole(UserRole.Admin.ToString()))
            {
                // Allow assigning lessons to both Teachers and Admins
                var teachers = await _db.Users.Where(u => (u.Role == UserRole.Teacher || u.Role == UserRole.Admin) && u.IsActive)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
                ViewBag.Teachers = teachers;
            }

            return View("Edit", vm);
        }

        // POST: /Lessons/EditLesson
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> EditLesson(CreateEditVm vm)
        {
            var currentUserId = _userService.GetCurrentUserId(User);
            if (currentUserId == null) return Unauthorized();

            var existingLesson = await _db.Lessons.FirstOrDefaultAsync(l => l.Id == vm.Id);
            if (existingLesson == null) return NotFound();

            var isAdmin = User.IsInRole(UserRole.Admin.ToString());
            if (!isAdmin && existingLesson.TeacherId != currentUserId.Value)
            {
                return Forbid();
            }

            // For non-admin users, ensure TeacherId remains the same
            if (!isAdmin)
            {
                vm.TeacherId = existingLesson.TeacherId;
            }

            if (vm.TeacherId == Guid.Empty)
            {
                ModelState.AddModelError("TeacherId", "Öğretmen seçmelisiniz.");
            }

            if (string.IsNullOrWhiteSpace(vm.Title))
            {
                ModelState.AddModelError("Title", "Ders başlığı gereklidir.");
            }

            if (vm.DayOfWeek < 1 || vm.DayOfWeek > 7)
            {
                ModelState.AddModelError("DayOfWeek", "Geçerli bir gün seçin.");
            }

            if (vm.StartTime == TimeSpan.Zero)
            {
                ModelState.AddModelError("StartTime", "Başlangıç saati gereklidir.");
            }

            if (vm.EndTime == TimeSpan.Zero)
            {
                ModelState.AddModelError("EndTime", "Bitiş saati gereklidir.");
            }

            if (vm.EndTime <= vm.StartTime)
            {
                ModelState.AddModelError("EndTime", "Bitiş saati başlangıç saatinden sonra olmalıdır.");
            }

            if (!await _db.Groups.AnyAsync(g => g.Id == vm.GroupId))
            {
                ModelState.AddModelError("GroupId", "Seçilen grup bulunamadı.");
            }

            if (vm.TeacherId != Guid.Empty && !await _db.Users.AnyAsync(u => u.Id == vm.TeacherId))
            {
                ModelState.AddModelError("TeacherId", "Seçilen öğretmen bulunamadı.");
            }

            if (!ModelState.IsValid)
            {
                var groups = await _db.Groups.OrderBy(g => g.Name).ToListAsync();
                ViewBag.Groups = groups;

                if (User.IsInRole(UserRole.Admin.ToString()))
                {
                    var teachers = await _db.Users.Where(u => u.Role == UserRole.Teacher).OrderBy(u => u.FullName).ToListAsync();
                    ViewBag.Teachers = teachers;
                }

                return View("Edit", vm);
            }

            existingLesson.Title = vm.Title;
            existingLesson.DayOfWeek = vm.DayOfWeek;
            existingLesson.StartTime = vm.StartTime;
            existingLesson.EndTime = vm.EndTime;
            existingLesson.GroupId = vm.GroupId;
            existingLesson.TeacherId = vm.TeacherId;
            existingLesson.IsActive = true; // Always keep lessons active

            await _db.SaveChangesAsync();

            TempData["Success"] = "Ders başarıyla güncellendi.";
            return RedirectToAction("Index");
        }

        // POST: /Lessons/DeleteLesson
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> DeleteLesson(Guid id)

        {
            var currentUserId = _userService.GetCurrentUserId(User);
            if (currentUserId == null) return Unauthorized();

            var lesson = await _db.Lessons.FirstOrDefaultAsync(l => l.Id == id);
            if (lesson == null) return NotFound();

            var isAdmin = User.IsInRole(UserRole.Admin.ToString());
            if (!isAdmin && lesson.TeacherId != currentUserId.Value)
            {
                return Forbid();
            }

            _db.Lessons.Remove(lesson);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Ders başarıyla silindi.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> OpenSession(Guid lessonId)
        {
            var currentUserId = _userService.GetCurrentUserId(User);
            if (currentUserId == null) return Unauthorized();

            var lesson = await _db.Lessons
                .Include(l => l.Group)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null) return NotFound();

            // Öğretmen kontrolü
            if (!User.IsInRole(UserRole.Admin.ToString()) && lesson.TeacherId != currentUserId)
                return Forbid();

            // Bugünün tarihini kontrol et
            var today = DateTimeOffset.Now.Date;
            var todayDayOfWeek = (int)today.DayOfWeek;
            if (todayDayOfWeek == 0) todayDayOfWeek = 7; // Pazar = 7

            if (lesson.DayOfWeek != todayDayOfWeek)
                return BadRequest("Bu ders bugün değil!");

            // Zaten açık oturum var mı kontrol et
            var existingSession = await _db.AttendanceSessions
                .FirstOrDefaultAsync(s => s.LessonId == lesson.Id && 
                    s.ScheduledAt >= today && s.ScheduledAt < today.AddDays(1));

            if (existingSession != null)
            {
                if (existingSession.Status == SessionStatus.Open)
                    return BadRequest("Bu ders için zaten açık bir oturum var!");
                else if (existingSession.Status == SessionStatus.Closed)
                    return BadRequest("Bu ders için zaten kapatılmış bir oturum var!");
            }

            // Yeni oturum oluştur
            var session = new AttendanceSession
            {
                LessonId = lesson.Id,
                ScheduledAt = ComputeNextOccurrenceWithTime(lesson.DayOfWeek, lesson.StartTime),
                Status = SessionStatus.Open,
                CreatedAt = DateTime.Now
            };

            _db.AttendanceSessions.Add(session);
            await _db.SaveChangesAsync();

            return RedirectToAction("Session", "Attendance", new { sessionId = session.Id });
        }

        private static DateTimeOffset ComputeNextOccurrenceWithTime(int lessonDayOfWeek, TimeSpan startTime)
        {
            // ISO day: Monday=1 .. Sunday=7
            var today = DateTimeOffset.Now;
            int todayIso = today.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)today.DayOfWeek;
            int deltaDays = (lessonDayOfWeek - todayIso + 7) % 7;

            var targetDate = today.Date.AddDays(deltaDays).Add(startTime);
            // If lesson is today and time has already passed, keep today at the specified start time (we still allow opening)
            return new DateTimeOffset(targetDate, today.Offset);
        }
    }
}
