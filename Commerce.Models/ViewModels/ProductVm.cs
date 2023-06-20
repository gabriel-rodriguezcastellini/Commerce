using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Commerce.Models.ViewModels
{
    public class ProductVm
    {
        public Product Product { get; set; } = null!;

        [ValidateNever]
        public IEnumerable<SelectListItem> CategoryList { get; set; } = null!;
    }
}
