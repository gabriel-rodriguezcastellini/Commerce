using Microsoft.AspNetCore.Mvc.Rendering;

namespace Commerce.Models.ViewModels
{
    public class RoleManagementVm
    {
        public ApplicationUser ApplicationUser { get; set; } = null!;
        public IEnumerable<SelectListItem> RoleList { get; set; } = null!;
        public IEnumerable<SelectListItem> CompanyList { get; set; } = null!;
    }
}
