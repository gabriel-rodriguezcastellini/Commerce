using Commerce.DataAccess.Repository.IRepository;
using Commerce.Models;
using Commerce.Models.ViewModels;
using Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace CommerceWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IWebHostEnvironment webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            this.unitOfWork = unitOfWork;
            this.webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index() => View(unitOfWork.ProductRepository.GetAll(includeProperties: "Category").ToList());

        public IActionResult UpdateInsert(int? id)
        {
            var productVm = new ProductVm
            {
                Product = new(),
                CategoryList = unitOfWork.CategoryRepository.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                })
            };
            if (id == null || id == 0)
            {
                return View(productVm);
            }
            else
            {
                productVm.Product = unitOfWork.ProductRepository.Get(u => u.Id == id, includeProperties: "ProductImages");
                return View(productVm);
            }
        }

        [HttpPost]
        public IActionResult UpdateInsert(ProductVm productVm, List<IFormFile> files)
        {
            if (ModelState.IsValid)
            {
                if (productVm.Product.Id == 0)
                {
                    unitOfWork.ProductRepository.Add(productVm.Product);
                }
                else
                {
                    unitOfWork.ProductRepository.Update(productVm.Product);
                }
                unitOfWork.Save();
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var productPath = @"images\products\product-" + productVm.Product.Id;
                        var finalPath = Path.Combine(webHostEnvironment.WebRootPath, productPath);
                        if (!Directory.Exists(finalPath))
                        {
                            Directory.CreateDirectory(finalPath);
                        }
                        using var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create);
                        file.CopyTo(fileStream);
                        var productImage = new ProductImage
                        {
                            ImageUrl = @"\" + productPath + @"\" + fileName,
                            ProductId = productVm.Product.Id
                        };
                        if (productVm.Product.ProductImages == null)
                        {
                            productVm.Product.ProductImages = new List<ProductImage>();
                        }
                        productVm.Product.ProductImages.Add(productImage);
                    }
                    unitOfWork.ProductRepository.Update(productVm.Product);
                    unitOfWork.Save();
                }
                TempData["success"] = "Product created/updated successfully";
                return RedirectToAction("Index");
            }
            else
            {
                productVm.CategoryList = unitOfWork.CategoryRepository.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
                return View(productVm);
            }
        }

        public IActionResult DeleteImage(int imageId)
        {
            var image = unitOfWork.ProductImageRepository.Get(x => x.Id == imageId);
            if (image != null)
            {
                if (!string.IsNullOrEmpty(image.ImageUrl))
                {
                    var oldImagePath = Path.Combine(webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('\\'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }
                unitOfWork.ProductImageRepository.Remove(image);
                unitOfWork.Save();
                TempData["success"] = "Deleted successfully";
            }
            return RedirectToAction(nameof(UpdateInsert), new { id = image!.ProductId });
        }

        [HttpGet]
        public IActionResult GetAll() => Json(new
        {
            data = unitOfWork.ProductRepository.GetAll(includeProperties: "Category").ToList()
        });

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productToBeDeleted = unitOfWork.ProductRepository.Get(u => u.Id == id);
            if (productToBeDeleted == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Error while deleting"
                });
            }                        
            var finalPath = Path.Combine(webHostEnvironment.WebRootPath, @"images\products\product-" + id);
            if (Directory.Exists(finalPath))
            {
                foreach (var filePath in Directory.GetFiles(finalPath))
                {
                    System.IO.File.Delete(filePath);
                }
                Directory.Delete(finalPath);
            }
            unitOfWork.ProductRepository.Remove(productToBeDeleted);
            unitOfWork.Save();
            return Json(new
            {
                success = true,
                message = "Delete Successful"
            });
        }
    }
}
