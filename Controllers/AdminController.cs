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
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _db;

        public AdminController(IUserService userService, AppDbContext db)
        {
            _userService = userService;
            _db = db;
        }

        // Dashboard
        public async Task<IActionResult> Index()
        {
            var stats = new
            {
                TotalUsers = await _db.Users.CountAsync(),
                ActiveUsers = await _db.Users.CountAsync(u => u.IsActive),
                TotalStudents = await _db.Students.CountAsync(),
                ActiveStudents = await _db.Students.CountAsync(s => s.IsActive),
                TotalGroups = await _db.Groups.CountAsync(),
                TotalLessons = await _db.Lessons.CountAsync(l => l.IsActive)
            };

            ViewBag.Stats = stats;
            return View();
        }

        #region User Management

        public async Task<IActionResult> Users()
        {
            var users = await _userService.GetAllUsersAsync();
            var vm = new UserListVm
            {
                Users = users.Select(u => new UserListItemVm
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    FullName = u.FullName,
                    Role = u.Role,
                    IsActive = u.IsActive
                }).OrderByDescending(r => r.Role == UserRole.Admin).ToList(),
                AdminCount = users.Count(u => u.Role == UserRole.Admin && u.IsActive)
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            var availableLessons = await _db.Lessons
                .Where(l => l.IsActive)
                .GroupBy(l => l.Title)
                .Select(g => new LessonSelectVm
                {
                    Id = g.First().Id,
                    Title = g.Key,
                    GroupName = "" // Grup bilgisi gösterilmiyor
                })
                .OrderBy(l => l.Title)
                .ToListAsync();

            var vm = new UserCreateEditVm
            {
                AvailableLessons = availableLessons
            };
            return View("EditUser", vm);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Kullanıcı bulunamadı.";
                return RedirectToAction(nameof(Users));
            }

            var availableLessons = await _db.Lessons
                .Where(l => l.IsActive)
                .GroupBy(l => l.Title)
                .Select(g => new LessonSelectVm
                {
                    Id = g.First().Id,
                    Title = g.Key,
                    GroupName = "" // Grup bilgisi gösterilmiyor
                })
                .OrderBy(l => l.Title)
                .ToListAsync();

            var assignedLessonIds = await _db.Lessons
                .Where(l => l.TeacherId == id && l.IsActive)
                .Select(l => l.Id)
                .ToListAsync();

            var vm = new UserCreateEditVm
            {
                Id = user.Id,
                UserName = user.UserName,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = user.IsActive,
                AvailableLessons = availableLessons,
                AssignedLessonIds = assignedLessonIds
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUser(UserCreateEditVm vm)
        {
            if (!ModelState.IsValid)
            {
                // Reload lessons for the view
                var availableLessons = await _db.Lessons
                    .Where(l => l.IsActive)
                    .GroupBy(l => l.Title)
                    .Select(g => new LessonSelectVm
                    {
                        Id = g.First().Id,
                        Title = g.Key,
                        GroupName = "" // Grup bilgisi gösterilmiyor
                    })
                    .OrderBy(l => l.Title)
                    .ToListAsync();
                vm.AvailableLessons = availableLessons;
                return View("EditUser", vm);
            }

            try
            {
                Guid userId;
                if (vm.IsEdit)
                {
                    // Update existing user
                    var user = await _userService.UpdateUserAsync(vm.Id!.Value, vm.UserName, vm.FullName, vm.Role, vm.IsActive);
                    if (user == null)
                    {
                        TempData["Error"] = "Kullanıcı bulunamadı.";
                        return RedirectToAction(nameof(Users));
                    }

                    // Change password if provided
                    if (!string.IsNullOrWhiteSpace(vm.Password))
                    {
                        await _userService.ChangePasswordAsync(vm.Id.Value, vm.Password);
                    }

                    userId = vm.Id.Value;
                    TempData["Success"] = "Kullanıcı başarıyla güncellendi.";
                }
                else
                {
                    // Create new user  
                    if (string.IsNullOrWhiteSpace(vm.Password))
                    {
                        ModelState.AddModelError("Password", "Yeni kullanıcı için şifre gereklidir.");
                        return View("EditUser", vm);
                    }

                    var newUser = await _userService.CreateUserAsync(vm.UserName, vm.FullName, vm.Password, vm.Role);
                    userId = newUser.Id;
                    TempData["Success"] = "Kullanıcı başarıyla oluşturuldu.";
                }

                // Update lesson assignments
                var currentAssigned = await _db.Lessons
                    .Where(l => l.TeacherId == userId && l.IsActive)
                    .ToListAsync();

                


                var toAdd = vm.AssignedLessonIds.Where(id => !currentAssigned.Any(l => l.Id == id)).ToList();

                foreach (var lessonId in toAdd)
                {
                    var lesson = await _db.Lessons.FindAsync(lessonId);
                    if (lesson != null)
                    {
                        lesson.TeacherId = userId;
                    }
                }

                await _db.SaveChangesAsync();

                return RedirectToAction(nameof(Users));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                // Reload lessons for the view
                var availableLessons = await _db.Lessons
                    .Where(l => l.IsActive)
                    .GroupBy(l => l.Title)
                    .Select(g => new LessonSelectVm
                    {
                        Id = g.First().Id,
                        Title = g.Key,
                        GroupName = "" // Grup bilgisi gösterilmiyor
                    })
                    .OrderBy(l => l.Title)
                    .ToListAsync();
                vm.AvailableLessons = availableLessons;
                return View("EditUser", vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                Guid? reassignTo = null;
                if (Request.HasFormContentType)
                {
                    var raw = Request.Form["reassignTo"].ToString();
                    if (Guid.TryParse(raw, out var parsed) && parsed != Guid.Empty)
                    {
                        reassignTo = parsed;
                    }
                }

                var success = await _userService.DeleteUserAsync(id, reassignTo);
                if (success)
                {
                    TempData["Success"] = "Kullanıcı başarıyla silindi.";
                }
                else
                {
                    TempData["Error"] = "Kullanıcı bulunamadı.";
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Users));
        }

        #endregion

        #region Student Management

        public async Task<IActionResult> Students(Guid? groupId = null, string? search = null, int page = 1, int pageSize = 10)
        {
            var query = _db.Students.Include(s => s.Group).AsQueryable();
            if (groupId.HasValue)
            {
                query = query.Where(s => s.GroupId == groupId.Value);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(s =>
                    (s.FirstName + " " + s.LastName).ToLower().Contains(term) ||
                    s.StudentNumber.ToLower().Contains(term));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(s => s.Group.Name)
                .ThenBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var groups = await _db.Groups.OrderBy(g => g.Name).ToListAsync();

            // Toplam istatistikler için ayrı sorgular
            var totalStudents = await _db.Students.CountAsync();
            var activeStudents = await _db.Students.CountAsync(s => s.IsActive);
            var inactiveStudents = totalStudents - activeStudents;

            // Filtreli istatistikler
            var filteredActiveStudents = await query.CountAsync(s => s.IsActive);
            var filteredInactiveStudents = total - filteredActiveStudents;

            var vm = new StudentListVm
            {
                Students = items.Select(s => new StudentListItemVm
                {
                    StudentId = s.Id,
                    GroupId = s.GroupId,
                    FullName = s.FullName,
                    StudentNumber = s.StudentNumber,
                    IsActive = s.IsActive
                }).ToList(),
                Groups = groups.Select(g => new GroupSelectItemVm
                {
                    Id = g.Id,
                    Name = g.Name,
                    Code = g.Code
                }).ToList(),
                SelectedGroupId = groupId,
                Search = search,
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                // İstatistikler - filtre varsa filtreli, yoksa toplam
                TotalStudentsCount = string.IsNullOrWhiteSpace(search) && !groupId.HasValue ? totalStudents : total,
                ActiveStudentsCount = string.IsNullOrWhiteSpace(search) && !groupId.HasValue ? activeStudents : filteredActiveStudents,
                InactiveStudentsCount = string.IsNullOrWhiteSpace(search) && !groupId.HasValue ? inactiveStudents : filteredInactiveStudents
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateStudent()
        {
            var groups = await _db.Groups.OrderBy(g => g.Name).ToListAsync();
            var vm = new StudentCreateEditVm
            {
                Groups = groups.Select(g => new GroupSelectItemVm
                {
                    Id = g.Id,
                    Name = g.Name,
                    Code = g.Code
                }).ToList()
            };

            return View("EditStudent", vm);
        }

        [HttpGet]
        public async Task<IActionResult> EditStudent(Guid id)
        {
            var student = await _userService.GetStudentByIdAsync(id);
            if (student == null)
            {
                TempData["Error"] = "Öğrenci bulunamadı.";
                return RedirectToAction(nameof(Students));
            }

            var groups = await _db.Groups.OrderBy(g => g.Name).ToListAsync();
            var vm = new StudentCreateEditVm
            {
                Id = student.Id,
                FirstName = student.FirstName,
                LastName = student.LastName,
                StudentNumber = student.StudentNumber,
                Phone = student.Phone,
                GroupId = student.GroupId,
                IsActive = student.IsActive,
                Groups = groups.Select(g => new GroupSelectItemVm
                {
                    Id = g.Id,
                    Name = g.Name,
                    Code = g.Code
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveStudent(StudentCreateEditVm vm)
        {
            if (!ModelState.IsValid)
            {
                // Reload groups for dropdown
                var groups = await _db.Groups.OrderBy(g => g.Name).ToListAsync();
                vm.Groups = groups.Select(g => new GroupSelectItemVm
                {
                    Id = g.Id,
                    Name = g.Name,
                    Code = g.Code
                }).ToList();

                return View("EditStudent", vm);
            }

            try
            {
                if (vm.IsEdit)
                {
                    // Update existing student
                    var student = await _userService.UpdateStudentAsync(vm.Id!.Value, vm.FirstName, vm.LastName, vm.StudentNumber, vm.Phone, vm.GroupId, vm.IsActive);
                    if (student == null)
                    {
                        TempData["Error"] = "Öğrenci bulunamadı.";
                        return RedirectToAction(nameof(Students));
                    }

                    TempData["Success"] = "Öğrenci başarıyla güncellendi.";
                }
                else
                {
                    // Create new student
                    await _userService.CreateStudentAsync(vm.FirstName, vm.LastName, vm.StudentNumber, vm.Phone, vm.GroupId);
                    TempData["Success"] = "Öğrenci başarıyla oluşturuldu.";
                }

                return RedirectToAction(nameof(Students));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                // Reload groups for dropdown
                var groups = await _db.Groups.OrderBy(g => g.Name).ToListAsync();
                vm.Groups = groups.Select(g => new GroupSelectItemVm
                {
                    Id = g.Id,
                    Name = g.Name,
                    Code = g.Code
                }).ToList();

                return View("EditStudent", vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudent(Guid id)
        {
            try
            {
                var success = await _userService.DeleteStudentAsync(id);
                if (success)
                {
                    TempData["Success"] = "Öğrenci başarıyla silindi.";
                }
                else
                {
                    TempData["Error"] = "Öğrenci bulunamadı.";
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Students));
        }

        #endregion
    }
}
