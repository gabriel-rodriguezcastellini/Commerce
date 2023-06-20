using Commerce.DataAccess.Repository.IRepository;
using Commerce.Models;
using Commerce.Models.ViewModels;
using Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace CommerceWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork unitOfWork;

        [BindProperty]
        public OrderVm OrderVm { get; set; } = null!;

        public OrderController(IUnitOfWork unitOfWork) => this.unitOfWork = unitOfWork;

        public IActionResult Index() => View();

        public IActionResult Details(int orderId)
        {
            OrderVm = new()
            {
                OrderHeader = unitOfWork.OrderHeaderRepository.Get(x => x.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetails = unitOfWork.OrderDetailRepository.GetAll(x => x.OrderHeaderId == orderId, includeProperties: "Product")
            };
            return View(OrderVm);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {
            var order = unitOfWork.OrderHeaderRepository.Get(x => x.Id == OrderVm.OrderHeader.Id);
            order.Name = OrderVm.OrderHeader.Name;
            order.PhoneNumber = OrderVm.OrderHeader.PhoneNumber;
            order.StreetAddress = OrderVm.OrderHeader.StreetAddress;
            order.City = OrderVm.OrderHeader.City;
            order.State = OrderVm.OrderHeader.State;
            order.PostalCode = OrderVm.OrderHeader.PostalCode;
            if (!string.IsNullOrEmpty(OrderVm.OrderHeader.Carrier))
            {
                order.Carrier = OrderVm.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderVm.OrderHeader.TrackingNumber))
            {
                order.TrackingNumber = OrderVm.OrderHeader.TrackingNumber;
            }
            unitOfWork.OrderHeaderRepository.Update(order);
            unitOfWork.Save();
            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = order.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            unitOfWork.OrderHeaderRepository.UpdateStatus(OrderVm.OrderHeader.Id, SD.StatusInProcess);
            unitOfWork.Save();
            TempData["success"] = "Order details updated successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderVm.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var order = unitOfWork.OrderHeaderRepository.Get(x => x.Id == OrderVm.OrderHeader.Id);
            order.TrackingNumber = OrderVm.OrderHeader.TrackingNumber;
            order.Carrier = OrderVm.OrderHeader.Carrier;
            order.OrderStatus = SD.StatusShipped;
            order.ShippingDate = DateTime.Now;
            if (order.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                order.PaymentDueDate = DateTime.Now.AddDays(30);
            }
            unitOfWork.OrderHeaderRepository.Update(order);
            unitOfWork.Save();
            TempData["success"] = "Order shipped successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderVm.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var order = unitOfWork.OrderHeaderRepository.Get(x => x.Id == OrderVm.OrderHeader.Id);
            if (order.PaymentStatus == SD.PaymentStatusApproved)
            {
                new RefundService().Create(new RefundCreateOptions { Reason = RefundReasons.RequestedByCustomer, PaymentIntent = order.PaymentIntentId });
                unitOfWork.OrderHeaderRepository.UpdateStatus(order.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                unitOfWork.OrderHeaderRepository.UpdateStatus(order.Id, SD.StatusCancelled, SD.StatusCancelled);
            }
            unitOfWork.Save();
            TempData["success"] = "Order cancelled successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderVm.OrderHeader.Id });
        }

        [ActionName("Details")]
        [HttpPost]
        public IActionResult Details_PAY_NOW()
        {
            OrderVm.OrderHeader = unitOfWork.OrderHeaderRepository.Get(x => x.Id == OrderVm.OrderHeader.Id, includeProperties: "ApplicationUser");
            OrderVm.OrderDetails = unitOfWork.OrderDetailRepository.GetAll(x => x.OrderHeaderId == OrderVm.OrderHeader.Id, includeProperties: "Product");
            var domain = Request.Scheme + "://" + Request.Host.Value + "/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVm.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/details?orderId={OrderVm.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };
            options.LineItems.AddRange(OrderVm.OrderDetails.Select(item => new SessionLineItemOptions
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
            unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(OrderVm.OrderHeader.Id, session.Id, session.PaymentIntentId);
            unitOfWork.Save();
            Response.Headers.Append("Location", session.Url);
            return new StatusCodeResult(303);
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            var orderHeader = unitOfWork.OrderHeaderRepository.Get(x => x.Id == orderHeaderId);
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                var session = service.Get(orderHeader.SessionId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                    unitOfWork.OrderHeaderRepository.UpdateStatus(orderHeaderId, orderHeader.OrderStatus!, SD.PaymentStatusApproved);
                    unitOfWork.Save();
                }
            }
            return View(orderHeaderId);
        }

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> orderHeaders;
            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orderHeaders = unitOfWork.OrderHeaderRepository.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                orderHeaders = unitOfWork.OrderHeaderRepository
                    .GetAll(x => x.ApplicationUserId == ((ClaimsIdentity)User.Identity!).FindFirst(ClaimTypes.NameIdentifier)!.Value, includeProperties: "ApplicationUser");
            }
            switch (status)
            {
                case "pending":
                    orderHeaders = orderHeaders.Where(x => x.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inProcess":
                    orderHeaders = orderHeaders.Where(x => x.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    orderHeaders = orderHeaders.Where(x => x.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    orderHeaders = orderHeaders.Where(x => x.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;
            }
            return Json(new
            {
                data = orderHeaders
            });
        }
    }
}
