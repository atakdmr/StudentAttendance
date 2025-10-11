using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Yoklama.Models.ViewModels;
using Yoklama.Services;
using System.Security.Claims;
using Yoklama.Data;
using Microsoft.EntityFrameworkCore;

namespace Yoklama.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _db;
        public AccountController(IUserService userService, AppDbContext db)
        {
            _userService = userService;
            _db = db;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Truncate returnUrl if too long to prevent 414 URI too long error
            if (returnUrl != null && returnUrl.Length > 200) returnUrl = null;
            return View(new LoginVm { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userService.AuthenticateAsync(vm.UserName, vm.Password);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
                return View(vm);
            }

            await _userService.SignInAsync(HttpContext, user, isPersistent: true);

            if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);

            // Admin kullanıcıları Admin/Index'e yönlendir
            if (user.Role == Models.Entities.UserRole.Admin)
                return RedirectToAction("Index", "Admin");

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _userService.SignOutAsync(HttpContext);
            return RedirectToAction("Login");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            ViewData["Title"] = "Hesabım";
            var userId = _userService.GetCurrentUserId(User);
            if (userId == null) return RedirectToAction("Login");

            var user = await _userService.GetByIdAsync(userId.Value);
            if (user == null) return RedirectToAction("Login");

            // Build activity data from latest audit logs for this user
            // Using Db through _userService is not available; we can rely on HttpContext's injected Db in layout, but here we'll fetch minimal via service extension in future.
            // For now, read last 10 entries via a small helper inside this controller if Db was injected; since not, skip if unavailable.
            var vm = new ProfilePageVm
            {
                Profile = new ProfileVm
                {
                    UserName = user.UserName,
                    FullName = user.FullName
                },
                Role = user.Role.ToString(),
                DisplayName = user.FullName
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([Bind(Prefix = "Profile")] ProfileVm profile)
        {
            if (!ModelState.IsValid)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
                var display = User.FindFirst("FullName")?.Value ?? (User.Identity?.Name ?? string.Empty);
                return View("Profile", new ProfilePageVm { Profile = profile, Role = role, DisplayName = display });
            }

            var userId = _userService.GetCurrentUserId(User);
            if (userId == null) return RedirectToAction("Login");

            try
            {
                // Only allow updating own username/fullname; keep role and active state unchanged
                var current = await _userService.GetByIdAsync(userId.Value);
                if (current == null)
                {
                    TempData["Error"] = "Kullanıcı bulunamadı.";
                    return RedirectToAction("Login");
                }

                var updated = await _userService.UpdateUserAsync(userId.Value, profile.UserName, profile.FullName, current.Role, current.IsActive);
                // UpdateUserAsync requires role and isActive; fetch current and keep them
                if (updated == null)
                {
                    TempData["Error"] = "Kullanıcı bulunamadı.";
                }
                else
                {
                    // Re-issue auth cookie to refresh claims like FullName and Name
                    await _userService.SignOutAsync(HttpContext);
                    await _userService.SignInAsync(HttpContext, updated, isPersistent: true);
                    TempData["Success"] = "Profil bilgileriniz güncellendi.";
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
                var display = User.FindFirst("FullName")?.Value ?? (User.Identity?.Name ?? string.Empty);
                return View("Profile", new ProfilePageVm { Profile = profile, Role = role, DisplayName = display });
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([Bind(Prefix = "ChangePassword")] ChangePasswordVm vm)
        {
            if (!ModelState.IsValid)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
                var display = User.FindFirst("FullName")?.Value ?? (User.Identity?.Name ?? string.Empty);
                return View("Profile", new ProfilePageVm { ChangePassword = vm, Profile = new ProfileVm { UserName = User.Identity?.Name ?? string.Empty, FullName = display }, Role = role, DisplayName = display });
            }

            var userId = _userService.GetCurrentUserId(User);
            if (userId == null) return RedirectToAction("Login");

            // Verify current password by re-authenticating with current username
            var currentUser = await _userService.GetByIdAsync(userId.Value);
            if (currentUser == null) return RedirectToAction("Login");

            var auth = await _userService.AuthenticateAsync(currentUser.UserName, vm.CurrentPassword);
            if (auth == null)
            {
                ModelState.AddModelError(string.Empty, "Mevcut şifre yanlış.");
                var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
                var display = User.FindFirst("FullName")?.Value ?? (User.Identity?.Name ?? string.Empty);
                return View("Profile", new ProfilePageVm { ChangePassword = vm, Profile = new ProfileVm { UserName = currentUser.UserName, FullName = currentUser.FullName }, Role = role, DisplayName = display });
            }

            var ok = await _userService.ChangePasswordAsync(userId.Value, vm.NewPassword);
            if (ok)
            {
                TempData["Success"] = "Şifreniz güncellendi.";
                // Refresh sign-in
                await _userService.SignOutAsync(HttpContext);
                await _userService.SignInAsync(HttpContext, currentUser, isPersistent: true);
            }
            else
            {
                TempData["Error"] = "Şifre güncellenemedi.";
            }

            return RedirectToAction("Profile");
        }
    }
}
