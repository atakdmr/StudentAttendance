using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Yoklama.Data;
using Yoklama.Models.Entities;
using Yoklama.Services;

namespace Yoklama.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GroupsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IGroupService _groupService;
        private readonly IUserService _userService;

        public GroupsController(AppDbContext db, IGroupService groupService, IUserService userService)
        {
            _db = db;
            _groupService = groupService;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            var groups = await _db.Groups
                .Include(g => g.Students)
                .Include(g => g.Lessons)
                .OrderBy(g => g.Name)
                .ToListAsync();
            return View(groups);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var vm = await _groupService.GetGroupDetailAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Group group)
        {
            if (ModelState.IsValid)
            {
                await _groupService.CreateAsync(group);
                return RedirectToAction(nameof(Index));
            }
            return View(group);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var group = await _groupService.GetByIdAsync(id);
            if (group == null) return NotFound();
            return View(group);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Group group)
        {
            if (ModelState.IsValid)
            {
                await _groupService.UpdateAsync(group);
                TempData["Success"] = "Grup başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Grup güncellenirken hata oluştu.";
            return View(group);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var group = await _groupService.GetByIdAsync(id);
            if (group == null) return NotFound();
            return View(group);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _groupService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
