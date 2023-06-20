using Commerce.DataAccess.Repository.IRepository;
using Commerce.Models;
using Commerce.Models.ViewModels;
using Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace CommerceWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork unitOfWork;

        [BindProperty]
        public ShoppingCartVm ShoppingCartVm { get; set; } = null!;

        public CartController(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var userId = ((ClaimsIdentity)User.Identity!).FindFirst(ClaimTypes.NameIdentifier)!.Value;
            ShoppingCartVm = new ShoppingCartVm()
            {
                ShoppingCarts = unitOfWork.ShoppingCartRepository.GetAll(x => x.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };
            var productImages = unitOfWork.ProductImageRepository.GetAll();
            foreach (var item in ShoppingCartVm.ShoppingCarts)
            {
                item.Product.ProductImages = productImages.Where(x => x.ProductId == item.Product.Id).ToList();
                item.Price = GetPriceBasedOnQuantity(item);
                ShoppingCartVm.OrderHeader.OrderTotal += item.Price * item.Count;
            }
            return View(ShoppingCartVm);
        }

        public IActionResult Summary()
        {
            var userId = ((ClaimsIdentity)User.Identity!).FindFirst(ClaimTypes.NameIdentifier)!.Value;
            ShoppingCartVm = new ShoppingCartVm()
            {
                ShoppingCarts = unitOfWork.ShoppingCartRepository.GetAll(x => x.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };
            ShoppingCartVm.OrderHeader.ApplicationUser = unitOfWork.ApplicationUserRepository.Get(x => x.Id == userId);
            ShoppingCartVm.OrderHeader.Name = ShoppingCartVm.OrderHeader.ApplicationUser.Name;
            ShoppingCartVm.OrderHeader.PhoneNumber = ShoppingCartVm.OrderHeader.ApplicationUser.PhoneNumber!;
            ShoppingCartVm.OrderHeader.StreetAddress = ShoppingCartVm.OrderHeader.ApplicationUser.StreetAddress!;
            ShoppingCartVm.OrderHeader.City = ShoppingCartVm.OrderHeader.ApplicationUser.City!;
            ShoppingCartVm.OrderHeader.State = ShoppingCartVm.OrderHeader.ApplicationUser.State!;
            ShoppingCartVm.OrderHeader.PostalCode = ShoppingCartVm.OrderHeader.ApplicationUser.PostalCode!;
            foreach (var item in ShoppingCartVm.ShoppingCarts)
            {
                item.Price = GetPriceBasedOnQuantity(item);
                ShoppingCartVm.OrderHeader.OrderTotal += item.Price * item.Count;
            }
            return View(ShoppingCartVm);
        }

        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPOST()
        {
            var userId = ((ClaimsIdentity)User.Identity!).FindFirst(ClaimTypes.NameIdentifier)!.Value;
            ShoppingCartVm.ShoppingCarts = unitOfWork.ShoppingCartRepository.GetAll(x => x.ApplicationUserId == userId, includeProperties: "Product");
            ShoppingCartVm.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartVm.OrderHeader.ApplicationUserId = userId;
            var applicationUser = unitOfWork.ApplicationUserRepository.Get(x => x.Id == userId);
            foreach (var item in ShoppingCartVm.ShoppingCarts)
            {
                item.Price = GetPriceBasedOnQuantity(item);
                ShoppingCartVm.OrderHeader.OrderTotal += item.Price * item.Count;
            }
            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                ShoppingCartVm.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartVm.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                ShoppingCartVm.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartVm.OrderHeader.OrderStatus = SD.StatusApproved;
            }
            unitOfWork.OrderHeaderRepository.Add(ShoppingCartVm.OrderHeader);
            unitOfWork.Save();
            foreach (var item in ShoppingCartVm.ShoppingCarts)
            {
                unitOfWork.OrderDetailRepository.Add(new OrderDetail
                {
                    ProductId = item.ProductId,
                    OrderHeaderId = ShoppingCartVm.OrderHeader.Id,
                    Price = item.Price,
                    Count = item.Count
                });
                unitOfWork.Save();
            }
            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                var domain = Request.Scheme + "://" + Request.Host.Value + "/";
                var options = new SessionCreateOptions
                {
                    SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVm.OrderHeader.Id}",
                    CancelUrl = domain + "customer/cart/index",
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };
                options.LineItems.AddRange(ShoppingCartVm.ShoppingCarts.Select(item => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                }));
                var service = new SessionService();
                var session = service.Create(options);
                unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(ShoppingCartVm.OrderHeader.Id, session.Id, session.PaymentIntentId);
                unitOfWork.Save();
                Response.Headers.Append("Location", session.Url);
                return new StatusCodeResult(303);
            }
            return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVm.OrderHeader.Id });
        }

        public IActionResult OrderConfirmation(int id)
        {
            var orderHeader = unitOfWork.OrderHeaderRepository.Get(x => x.Id == id, includeProperties: "ApplicationUser");
            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                var session = service.Get(orderHeader.SessionId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                    unitOfWork.OrderHeaderRepository.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    unitOfWork.Save();
                }
                HttpContext.Session.Clear();
            }
            unitOfWork.ShoppingCartRepository.RemoveRange(unitOfWork.ShoppingCartRepository.GetAll(x => x.ApplicationUserId == orderHeader.ApplicationUserId).ToList());
            unitOfWork.Save();
            return View(id);
        }

        public IActionResult Plus(int cartId)
        {
            var cart = unitOfWork.ShoppingCartRepository.Get(x => x.Id == cartId);
            cart.Count++;
            unitOfWork.ShoppingCartRepository.Update(cart);
            unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            var cart = unitOfWork.ShoppingCartRepository.Get(x => x.Id == cartId, track: true);
            if (cart.Count <= 1)
            {
                HttpContext.Session.SetInt32(SD.SessionCart, unitOfWork.ShoppingCartRepository.GetAll(x => x.ApplicationUserId == cart.ApplicationUserId).Count() - 1);
                unitOfWork.ShoppingCartRepository.Remove(cart);
            }
            else
            {
                cart.Count--;
                unitOfWork.ShoppingCartRepository.Update(cart);
            }
            unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            var cart = unitOfWork.ShoppingCartRepository.Get(x => x.Id == cartId, track: true);
            HttpContext.Session.SetInt32(SD.SessionCart, unitOfWork.ShoppingCartRepository.GetAll(x => x.ApplicationUserId == cart.ApplicationUserId).Count() - 1);
            unitOfWork.ShoppingCartRepository.Remove(cart);
            unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        private static double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            return shoppingCart.Count <= 50
                ? shoppingCart.Product.Price
                : shoppingCart.Count <= 100 ? shoppingCart.Product.Price50 : shoppingCart.Product.Price100;
        }
    }
}
