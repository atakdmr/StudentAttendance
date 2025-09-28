using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Yoklama.Models;
using Yoklama.Data;
using Yoklama.Services;
using Microsoft.EntityFrameworkCore;
using Yoklama.Models.Entities;

namespace Yoklama.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _db;
    private readonly IUserService _userService;

    public HomeController(ILogger<HomeController> logger, AppDbContext db, IUserService userService)
    {
        _logger = logger;
        _db = db;
        _userService = userService;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity!.IsAuthenticated)
        {
            var currentUserId = _userService.GetCurrentUserId(User);
            if (currentUserId.HasValue)
            {
                var today = DateTimeOffset.Now.Date;
                if (User.IsInRole("Admin"))
                {
                    // Admin dashboard
                    var stats = new
                    {
                        GroupCount = await _db.Groups.CountAsync(),
                        StudentCount = await _db.Students.CountAsync(s => s.IsActive),
                        LessonCount = await _db.Lessons.CountAsync(l => l.IsActive),
                        UserCount = await _db.Users.CountAsync(u => u.IsActive)
                    };
                    ViewBag.Stats = stats;
                    ViewBag.IsAdmin = true;

                    var sessions = await _db.AttendanceSessions
                        .Include(s => s.Lesson)
                        .Include(s => s.Group)
                        .Include(s => s.Teacher)
                        .ToListAsync();
                    var sessionsToday = sessions
                        .Where(s => s.ScheduledAt >= today && s.ScheduledAt < today.AddDays(1))
                        .OrderBy(s => s.ScheduledAt)
                        .ToList();
                    ViewBag.SessionsToday = sessionsToday;
                }
                else if (User.IsInRole("Teacher"))
                {
                    // Teacher dashboard
                    var lessonCount = await _db.Lessons.CountAsync(l => l.TeacherId == currentUserId.Value && l.IsActive);
                    var sessionCount = await _db.AttendanceSessions.CountAsync(s => s.TeacherId == currentUserId.Value);
                    ViewBag.LessonCount = lessonCount;
                    ViewBag.SessionCount = sessionCount;
                    ViewBag.IsTeacher = true;

                    var sessions = await _db.AttendanceSessions
                        .Where(s => s.TeacherId == currentUserId.Value)
                        .Include(s => s.Lesson)
                        .Include(s => s.Group)
                        .ToListAsync();
                    var sessionsToday = sessions
                        .Where(s => s.ScheduledAt >= today && s.ScheduledAt < today.AddDays(1))
                        .OrderBy(s => s.ScheduledAt)
                        .ToList();
                    ViewBag.SessionsToday = sessionsToday;
                }
            }
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
