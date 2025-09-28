using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Yoklama.Data;
using Yoklama.Models.Entities;
using Yoklama.Models.ViewModels;

namespace Yoklama.Services
{
    public class GroupService : IGroupService
    {
        private readonly AppDbContext _db;

        public GroupService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Group>> GetAllAsync()
        {
            return await _db.Groups
                .AsNoTracking()
                .OrderBy(g => g.Name)
                .ToListAsync();
        }

        public async Task<Group?> GetByIdAsync(Guid id)
        {
            return await _db.Groups
                .Include(g => g.Lessons)
                .Include(g => g.Students)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<GroupDetailVm?> GetGroupDetailAsync(Guid id)
        {
            var group = await _db.Groups
                .AsNoTracking()
                .Include(g => g.Lessons).ThenInclude(l => l.Teacher)
                .Include(g => g.Students)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return null;

            var vm = new GroupDetailVm
            {
                GroupId = group.Id,
                Name = group.Name,
                Code = group.Code,
                Description = group.Description,
                Lessons = group.Lessons
                    .OrderBy(l => l.DayOfWeek)
                    .ThenBy(l => l.StartTime)
                    .Select(l => new LessonVm
                    {
                        Id = l.Id,
                        GroupId = l.GroupId,
                        Title = l.Title,
                        DayOfWeek = l.DayOfWeek,
                        StartTime = l.StartTime,
                        EndTime = l.EndTime,
                        TeacherId = l.TeacherId,
                        TeacherName = l.Teacher.FullName,
                        IsActive = l.IsActive
                    }).ToList(),
                Students = group.Students
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .Select(s => new StudentListItemVm
                    {
                        StudentId = s.Id,
                        FullName = s.FullName,
                        StudentNumber = s.StudentNumber,
                        IsActive = s.IsActive
                    }).ToList()
            };

            return vm;
        }

        public async Task<SidebarVm> GetSidebarVmAsync()
        {
            var groups = await _db.Groups
                .AsNoTracking()
                .Include(g => g.Lessons)
                .OrderBy(g => g.Name)
                .ToListAsync();

            var vm = new SidebarVm
            {
                Groups = groups.Select(g => new GroupSidebarItem
                {
                    GroupId = g.Id,
                    Name = g.Name,
                    Lessons = g.Lessons
                        .Where(l => l.IsActive)
                        .OrderBy(l => l.DayOfWeek)
                        .ThenBy(l => l.StartTime)
                        .Select(l => new LessonSidebarItem
                        {
                            LessonId = l.Id,
                            Title = l.Title,
                            StartTime = l.StartTime
                        }).ToList()
                }).ToList()
            };

            return vm;
        }

        public async Task CreateAsync(Group group)
        {
            _db.Groups.Add(group);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Group group)
        {
            _db.Groups.Update(group);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var group = await _db.Groups.FindAsync(id);
            if (group == null) return false;
            _db.Groups.Remove(group);
            await _db.SaveChangesAsync();
            return true;
        }

        // Lesson management
        public async Task CreateLessonAsync(Lesson lesson)
        {
            _db.Lessons.Add(lesson);
            await _db.SaveChangesAsync();
        }

        public async Task<Lesson?> GetLessonByIdAsync(Guid id)
        {
            return await _db.Lessons
                .Include(l => l.Group)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task UpdateLessonAsync(Lesson lesson)
        {
            _db.Lessons.Update(lesson);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteLessonAsync(Guid id)
        {
            var lesson = await _db.Lessons.FindAsync(id);
            if (lesson == null) return false;
            _db.Lessons.Remove(lesson);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
