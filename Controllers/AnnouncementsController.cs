using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Yoklama.Data;
using Yoklama.Models.Entities;
using Yoklama.Services;
using Microsoft.AspNetCore.Authorization;
using Yoklama.Models.ViewModels;

namespace Yoklama.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AnnouncementsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IUserService _userService;

        public AnnouncementsController(AppDbContext db, IUserService userService)
        {
            _db = db;
            _userService = userService;
        }

        // GET: Announcements
        public async Task<IActionResult> Index()
        {
            var announcements = await _db.Announcements
                .Include(a => a.CreatedBy)
                .OrderByDescending(a => a.Priority)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(announcements);
        }

        // GET: Announcements/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Announcements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AnnouncementCreateVm model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUserId = _userService.GetCurrentUserId(User);
            if (!currentUserId.HasValue)
            {
                TempData["Error"] = "Kullanıcı bilgisi bulunamadı.";
                return View(model);
            }

            try
            {
                var announcement = new Announcement
                {
                    Title = model.Title,
                    Content = model.Content,
                    IsActive = model.IsActive,
                    Priority = model.Priority,
                    CreatedById = currentUserId.Value,
                    CreatedAt = DateTime.Now
                };

                _db.Announcements.Add(announcement);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Duyuru başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Hata oluştu: {ex.Message}";
                return View(model);
            }
        }

        // GET: Announcements/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            var announcement = await _db.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }

        // POST: Announcements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Announcement announcement)
        {
            if (id != announcement.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    announcement.UpdatedAt = DateTime.Now;
                    _db.Update(announcement);
                    await _db.SaveChangesAsync();

                    TempData["Success"] = "Duyuru başarıyla güncellendi.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnnouncementExists(announcement.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            return View(announcement);
        }

        // GET: Announcements/Delete/5
        public async Task<IActionResult> Delete(Guid id)
        {
            var announcement = await _db.Announcements
                .Include(a => a.CreatedBy)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }

        // POST: Announcements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var announcement = await _db.Announcements.FindAsync(id);
            if (announcement != null)
            {
                _db.Announcements.Remove(announcement);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Duyuru başarıyla silindi.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Announcements/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var announcement = await _db.Announcements
                .Include(a => a.CreatedBy)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }

        private bool AnnouncementExists(Guid id)
        {
            return _db.Announcements.Any(e => e.Id == id);
        }
    }
}
