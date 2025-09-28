using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Yoklama.Data;
using Yoklama.Models.Entities;

namespace Yoklama.Services
{
    public class AuthService : IUserService
    {
        private readonly AppDbContext _db;
        private readonly PasswordHasher<User> _hasher = new();

        public AuthService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<User?> AuthenticateAsync(string userName, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserName == userName && x.IsActive);
            if (user == null) return null;

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed) return null;

            // If success and hash needs upgrade
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _hasher.HashPassword(user, password);
                await _db.SaveChangesAsync();
            }

            return user;
        }

        public async Task SignInAsync(HttpContext httpContext, User user, bool isPersistent = true)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProps = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                AllowRefresh = true
            };

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);
        }

        public async Task SignOutAsync(HttpContext httpContext)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<User?> GetByUserNameAsync(string userName)
        {
            return await _db.Users.FirstOrDefaultAsync(x => x.UserName == userName);
        }

        public async Task<List<User>> GetTeachersAsync()
        {
            return await _db.Users.Where(u => u.Role == UserRole.Teacher && u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        }

        public Guid? GetCurrentUserId(ClaimsPrincipal user)
        {
            var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idStr, out var id) ? id : null;
        }

        public bool IsInRole(ClaimsPrincipal user, UserRole role)
        {
            return user.IsInRole(role.ToString());
        }

        // Admin User Management
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _db.Users.OrderBy(u => u.FullName).ToListAsync();
        }

        public async Task<User> CreateUserAsync(string userName, string fullName, string password, UserRole role)
        {
            // Check if username already exists
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Bu kullanıcı adı zaten kullanılıyor.");
            }

            var user = new User
            {
                UserName = userName,
                FullName = fullName,
                Role = role,
                IsActive = true,
                PasswordHash = _hasher.HashPassword(new User(), password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        public async Task<User?> UpdateUserAsync(Guid id, string userName, string fullName, UserRole role, bool isActive)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return null;

            // Check if username is taken by another user
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.UserName == userName && u.Id != id);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Bu kullanıcı adı zaten kullanılıyor.");
            }

            user.UserName = userName;
            user.FullName = fullName;
            user.Role = role;
            user.IsActive = isActive;

            await _db.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUserAsync(Guid id, Guid? reassignToTeacherId = null)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return false;

            // Don't allow deleting the last admin
            if (user.Role == UserRole.Admin)
            {
                var adminCount = await _db.Users.CountAsync(u => u.Role == UserRole.Admin && u.IsActive);
                if (adminCount <= 1)
                {
                    throw new InvalidOperationException("Son admin kullanıcı silinemez.");
                }
            }

            // Reassign or block when the user (teacher or admin) is referenced by lessons/sessions
            var hasLessonsForUser = await _db.Lessons.AnyAsync(l => l.TeacherId == user.Id);
            var hasSessionsForUser = await _db.AttendanceSessions.AnyAsync(s => s.TeacherId == user.Id);

            if (hasLessonsForUser || hasSessionsForUser)
            {
                if (reassignToTeacherId.HasValue && reassignToTeacherId.Value != Guid.Empty)
                {
                    // Allow reassignment target to be any active user (Teacher or Admin)
                    var targetUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == reassignToTeacherId && u.IsActive);
                    if (targetUser == null)
                    {
                        throw new InvalidOperationException("Devredilecek kullanıcı bulunamadı.");
                    }

                    if (hasLessonsForUser)
                    {
                        var lessons = await _db.Lessons.Where(l => l.TeacherId == user.Id).ToListAsync();
                        foreach (var lesson in lessons)
                        {
                            lesson.TeacherId = targetUser.Id;
                        }
                    }

                    if (hasSessionsForUser)
                    {
                        var sessions = await _db.AttendanceSessions.Where(s => s.TeacherId == user.Id).ToListAsync();
                        foreach (var session in sessions)
                        {
                            session.TeacherId = targetUser.Id;
                        }
                    }
                }
                else
                {
                    // No reassignment provided; block delete because FK is Restrict
                    throw new InvalidOperationException("Bu kullanıcının bağlı ders veya yoklama oturumları var. Silmeden önce devretmelisiniz.");
                }
            }

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid id, string newPassword)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return false;

            user.PasswordHash = _hasher.HashPassword(user, newPassword);
            await _db.SaveChangesAsync();
            return true;
        }

        // Admin Student Management
        public async Task<List<Student>> GetAllStudentsAsync()
        {
            return await _db.Students
                .Include(s => s.Group)
                .OrderBy(s => s.Group.Name)
                .ThenBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        public async Task<List<Student>> GetStudentsByGroupAsync(Guid groupId)
        {
            return await _db.Students
                .Where(s => s.GroupId == groupId)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        public async Task<Student> CreateStudentAsync(string firstName, string lastName, string studentNumber, string? phone, Guid groupId)
        {
            // Check if student number already exists
            var existingStudent = await _db.Students.FirstOrDefaultAsync(s => s.StudentNumber == studentNumber);
            if (existingStudent != null)
            {
                throw new InvalidOperationException("Bu öğrenci numarası zaten kullanılıyor.");
            }

            // Check if group exists
            var group = await _db.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
            {
                throw new InvalidOperationException("Belirtilen grup bulunamadı.");
            }

            var student = new Student
            {
                FirstName = firstName,
                LastName = lastName,
                StudentNumber = studentNumber,
                Phone = phone,
                GroupId = groupId,
                IsActive = true
            };

            _db.Students.Add(student);
            await _db.SaveChangesAsync();
            return student;
        }

        public async Task<Student?> UpdateStudentAsync(Guid id, string firstName, string lastName, string studentNumber, string? phone, Guid groupId, bool isActive)
        {
            var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == id);
            if (student == null) return null;

            // Check if student number is taken by another student
            var existingStudent = await _db.Students.FirstOrDefaultAsync(s => s.StudentNumber == studentNumber && s.Id != id);
            if (existingStudent != null)
            {
                throw new InvalidOperationException("Bu öğrenci numarası zaten kullanılıyor.");
            }

            // Check if group exists
            var group = await _db.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
            {
                throw new InvalidOperationException("Belirtilen grup bulunamadı.");
            }

            student.FirstName = firstName;
            student.LastName = lastName;
            student.StudentNumber = studentNumber;
            student.Phone = phone;
            student.GroupId = groupId;
            student.IsActive = isActive;

            await _db.SaveChangesAsync();
            return student;
        }

        public async Task<bool> DeleteStudentAsync(Guid id)
        {
            var student = await _db.Students.Include(s => s.Group).FirstOrDefaultAsync(s => s.Id == id);
            if (student == null) return false;

            // Remove attendance records for this student to maintain referential integrity
            var records = await _db.AttendanceRecords.Where(r => r.StudentId == student.Id).ToListAsync();
            if (records.Count > 0)
            {
                _db.AttendanceRecords.RemoveRange(records);
            }

            _db.Students.Remove(student);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<Student?> GetStudentByIdAsync(Guid id)
        {
            return await _db.Students
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
    }
}
