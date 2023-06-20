using Commerce.DataAccess.Repository.IRepository;
using Commerce.Models;
using Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork unitOfWork;        

        public CompanyController(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;            
        }

        public IActionResult Index() => View(unitOfWork.CompanyRepository.GetAll().ToList());

        public IActionResult UpdateInsert(int? id)
        {            
            if (id == null || id == 0)
            {
                return View(new Company());
            }
            else
            {
                Company company = unitOfWork.CompanyRepository.Get(u => u.Id == id);
                return View(company);
            }
        }

        [HttpPost]
        public IActionResult UpdateInsert(Company company)
        {
            if (ModelState.IsValid)
            {
                if (company.Id == 0)
                {
                    unitOfWork.CompanyRepository.Add(company);
                }
                else
                {
                    unitOfWork.CompanyRepository.Update(company);
                }
                unitOfWork.Save();
                TempData["success"] = "Company created successfully";
                return RedirectToAction("Index");
            }
            else
            {
                return View(company);
            }
        }

        [HttpGet]
        public IActionResult GetAll() => Json(new
        {
            data = unitOfWork.CompanyRepository.GetAll().ToList()
        });

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var companyToBeDeleted = unitOfWork.CompanyRepository.Get(u => u.Id == id);
            if (companyToBeDeleted == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Error while deleting"
                });
            }
            unitOfWork.CompanyRepository.Remove(companyToBeDeleted);
            unitOfWork.Save();
            return Json(new
            {
                success = true,
                message = "Delete Successful"
            });
        }
    }
}
