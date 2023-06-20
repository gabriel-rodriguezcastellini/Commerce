using Commerce.DataAccess.Repository.IRepository;
using Commerce.Models;
using Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace CommerceWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index() => View(_unitOfWork.ProductRepository.GetAll(includeProperties: "Category,ProductImages").ToList());

        public IActionResult Details(int productId)
        {
            return View(new ShoppingCart
            {
                Product = _unitOfWork.ProductRepository.Get(u => u.Id == productId, includeProperties: "Category,ProductImages"),
                Count = 1,
                ProductId = productId
            });
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var userId = ((ClaimsIdentity)User.Identity!).FindFirst(ClaimTypes.NameIdentifier)!.Value;
            shoppingCart.ApplicationUserId = userId;
            var cartFromDb = _unitOfWork.ShoppingCartRepository.Get(x => x.ApplicationUserId == userId && x.ProductId == shoppingCart.ProductId);
            if (cartFromDb != null)
            {
                cartFromDb.Count += shoppingCart.Count;
                _unitOfWork.ShoppingCartRepository.Update(cartFromDb);
                _unitOfWork.Save();
            }
            else
            {
                _unitOfWork.ShoppingCartRepository.Add(shoppingCart);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCartRepository.GetAll(x => x.ApplicationUserId == userId).Count());
            }
            TempData["success"] = "Card updated successfully";            
            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}