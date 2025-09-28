using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Yoklama.Data;
using Yoklama.Services;

namespace Yoklama.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IUserService _userService;

        public NotificationsController(AppDbContext db, IUserService userService)
        {
            _db = db;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Bildirimler";
            var logs = await _db.AuditLogs
                .Include(x => x.User)
                .OrderByDescending(x => x.Timestamp)
                .ToListAsync();
            return View(logs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                // Mevcut kullanıcının ID'sini al
                var currentUserId = _userService.GetCurrentUserId(User);
                if (currentUserId == null)
                {
                    TempData["ErrorMessage"] = "Kullanıcı bilgisi bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                var userLogs = await _db.AuditLogs
                    .Where(x => x.UserId == currentUserId.Value)
                    .ToListAsync();

                if (userLogs.Any())
                {
                    _db.AuditLogs.RemoveRange(userLogs);
                    await _db.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"{userLogs.Count} bildirim başarıyla silindi.";
                }
                else
                {
                    TempData["InfoMessage"] = "Silinecek bildirim bulunamadı.";
                }
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Bildirimler silinirken bir hata oluştu: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}


