using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Yoklama.Models.Entities;
using Yoklama.Models.ViewModels;

namespace Yoklama.Services
{
    public interface IGroupService
    {
        Task<List<Group>> GetAllAsync();
        Task<Group?> GetByIdAsync(Guid id);
        Task<GroupDetailVm?> GetGroupDetailAsync(Guid id);
        Task<SidebarVm> GetSidebarVmAsync();
        Task CreateAsync(Group group);
        Task UpdateAsync(Group group);
        Task<bool> DeleteAsync(Guid id);

        // Lesson management
        Task CreateLessonAsync(Lesson lesson);
        Task<Lesson?> GetLessonByIdAsync(Guid id);
        Task UpdateLessonAsync(Lesson lesson);
        Task<bool> DeleteLessonAsync(Guid id);
    }
}
