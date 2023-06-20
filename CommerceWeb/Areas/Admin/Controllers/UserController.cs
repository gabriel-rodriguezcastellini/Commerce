using Commerce.DataAccess.Data;
using Commerce.DataAccess.Repository.IRepository;
using Commerce.Models.ViewModels;
using Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CommerceWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;

        public UserController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index() => View();

        public IActionResult RoleManagement(string userId)
        {
            var roleVm = new RoleManagementVm
            {
                ApplicationUser = _unitOfWork.ApplicationUserRepository.Get(x => x.Id == userId, includeProperties: "Company"),
                RoleList = _roleManager.Roles.Select(x => new SelectListItem { Text = x.Name, Value = x.Name }),
                CompanyList = _unitOfWork.CompanyRepository.GetAll().Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
            };
            roleVm.ApplicationUser.Role = _userManager.GetRolesAsync(_unitOfWork.ApplicationUserRepository.Get(x => x.Id == userId)).GetAwaiter().GetResult().FirstOrDefault()!;
            return View(roleVm);
        }

        [HttpPost]
        public IActionResult RoleManagement(RoleManagementVm roleManagementVm)
        {
            var oldRole = _userManager.GetRolesAsync(_unitOfWork.ApplicationUserRepository.Get(x => x.Id == roleManagementVm.ApplicationUser.Id)).GetAwaiter().GetResult().FirstOrDefault();
            var user = _unitOfWork.ApplicationUserRepository.Get(x => x.Id == roleManagementVm.ApplicationUser.Id);
            if (!(roleManagementVm.ApplicationUser.Role == oldRole))
            {
                if (roleManagementVm.ApplicationUser.Role == SD.Role_Company)
                {
                    user.CompanyId = roleManagementVm.ApplicationUser.CompanyId;
                }
                if (oldRole == SD.Role_Company)
                {
                    user.CompanyId = null;
                }
                _unitOfWork.ApplicationUserRepository.Update(user);
                _unitOfWork.Save();
                _userManager.RemoveFromRoleAsync(user, oldRole!).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(user, roleManagementVm.ApplicationUser.Role).GetAwaiter().GetResult();
            }
            else
            {
                if (oldRole == SD.Role_Company && user.CompanyId != roleManagementVm.ApplicationUser.CompanyId)
                {
                    user.CompanyId = roleManagementVm.ApplicationUser.CompanyId;
                    _unitOfWork.ApplicationUserRepository.Update(user);
                    _unitOfWork.Save();
                }
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _unitOfWork.ApplicationUserRepository.GetAll(includeProperties: "Company").ToList();
            foreach (var item in users)
            {
                item.Role = _userManager.GetRolesAsync(item).GetAwaiter().GetResult().FirstOrDefault()!;
                item.Company ??= new() { Name = "" };
            }
            return Json(new
            {
                data = users
            });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody] string id)
        {
            var user = _unitOfWork.ApplicationUserRepository.Get(x => x.Id == id);
            if (user == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking" });
            }
            user.LockoutEnd = user.LockoutEnd != null && user.LockoutEnd > DateTime.Now ? (DateTimeOffset?)DateTime.Now : (DateTimeOffset?)DateTime.Now.AddYears(1000);
            _unitOfWork.ApplicationUserRepository.Update(user);
            _unitOfWork.Save();            
            return Json(new { success = true, message = "Operation Successful" });
        }
    }
}
