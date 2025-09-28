using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Yoklama.Services;

namespace Yoklama.ViewComponents
{
    public class SidebarViewComponent : ViewComponent
    {
        private readonly IGroupService _groupService;

        public SidebarViewComponent(IGroupService groupService)
        {
            _groupService = groupService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var vm = await _groupService.GetSidebarVmAsync();
            return View(vm);
        }
    }
}
