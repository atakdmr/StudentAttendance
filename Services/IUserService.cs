using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Yoklama.Models.Entities;

namespace Yoklama.Services
{
    public interface IUserService
    {
        Task<User?> AuthenticateAsync(string userName, string password);
        Task SignInAsync(HttpContext httpContext, User user, bool isPersistent = true);
        Task SignOutAsync(HttpContext httpContext);

        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByUserNameAsync(string userName);
        Task<List<User>> GetTeachersAsync();

        Guid? GetCurrentUserId(ClaimsPrincipal user);
        bool IsInRole(ClaimsPrincipal user, UserRole role);

        // Admin User Management
        Task<List<User>> GetAllUsersAsync();
        Task<User> CreateUserAsync(string userName, string fullName, string password, UserRole role);
        Task<User?> UpdateUserAsync(Guid id, string userName, string fullName, UserRole role, bool isActive);
        Task<bool> DeleteUserAsync(Guid id, Guid? reassignToTeacherId = null);
        Task<bool> ChangePasswordAsync(Guid id, string newPassword);

        // Admin Student Management
        Task<List<Student>> GetAllStudentsAsync();
        Task<List<Student>> GetStudentsByGroupAsync(Guid groupId);
        Task<Student> CreateStudentAsync(string firstName, string lastName, string studentNumber, string? phone, Guid groupId);
        Task<Student?> UpdateStudentAsync(Guid id, string firstName, string lastName, string studentNumber, string? phone, Guid groupId, bool isActive);
        Task<bool> DeleteStudentAsync(Guid id);
        Task<Student?> GetStudentByIdAsync(Guid id);
    }
}
