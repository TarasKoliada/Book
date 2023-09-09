using BookWeb.DataAccess.Repository.IRepository;
using BookWeb.Models;
using BookWeb.Models.ViewModels;
using BookWeb.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BookWeb.Areas.Admin.Controllers
{
    [Area("admin")]
	[Authorize]
	public class OrderController : Controller
	{
		[BindProperty]
		public OrderVM OrderVm { get; set; }
		private readonly IUnitOfWork _unitOfWork;
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
		{
			return View();
		}
		public IActionResult Details(int orderId) 
		{
			OrderVm = new()
			{
				OrderHeader = _unitOfWork.OrderHeader.Get(orderHeader => orderHeader.Id == orderId, includeProperties: "ApplicationUser"),
				OrderDetail = _unitOfWork.OrderDetail.GetAll(orderDetail => orderDetail.OrderHeaderId == orderId, includeProperties: "Product")
			};
			return View(OrderVm);
		}

		[HttpPost]
		[Authorize(Roles = StaticDetails.Role_Admin +","+ StaticDetails.Role_Employee)]
		public IActionResult UpdateOrderDetails()
		{
			var orderToUpdate = _unitOfWork.OrderHeader.Get(o => o.Id == OrderVm.OrderHeader.Id);
			orderToUpdate.Name = OrderVm.OrderHeader.Name;
			orderToUpdate.PhoneNumber = OrderVm.OrderHeader.PhoneNumber;
			orderToUpdate.StreetAddress = OrderVm.OrderHeader.StreetAddress;
			orderToUpdate.City = OrderVm.OrderHeader.City;
			orderToUpdate.State = OrderVm.OrderHeader.State;
			orderToUpdate.PostalCode = OrderVm.OrderHeader.PostalCode;

			if (!string.IsNullOrEmpty(OrderVm.OrderHeader.Carrier))
				orderToUpdate.Carrier = OrderVm.OrderHeader.Carrier;

			if (!string.IsNullOrEmpty(OrderVm.OrderHeader.TrackingNumber))
				orderToUpdate.TrackingNumber = OrderVm.OrderHeader.TrackingNumber;

			_unitOfWork.OrderHeader.Update(orderToUpdate);
			_unitOfWork.Save();

			TempData["Success"] = "Order Details Updated Successfully";

			return RedirectToAction(nameof(Details), new { orderId = orderToUpdate.Id});
		}
		[HttpPost]
        [Authorize(Roles = StaticDetails.Role_Admin + "," + StaticDetails.Role_Employee)]
        public IActionResult StartProcessing() 
		{
			_unitOfWork.OrderHeader.UpdateStatus(OrderVm.OrderHeader.Id, StaticDetails.StatusInProcess);
			_unitOfWork.Save();

			TempData["Success"] = "Order Processing is Successfull";

			return RedirectToAction(nameof(Details), new { orderId = OrderVm.OrderHeader.Id});
		}

        [HttpPost]
        [Authorize(Roles = StaticDetails.Role_Admin + "," + StaticDetails.Role_Employee)]
        public IActionResult ShipOrder()
        {
			var order = _unitOfWork.OrderHeader.Get(o => o.Id == OrderVm.OrderHeader.Id);
			order.Carrier = OrderVm.OrderHeader.Carrier;
			order.TrackingNumber = OrderVm.OrderHeader.TrackingNumber;
			order.OrderStatus = StaticDetails.StatusShipped;
			order.ShippingDate = DateTime.Now;
			if (order.PaymentStatus == StaticDetails.PaymentStatusDelayedPayment)
				order.PaymentDueDate = DateTime.Now.AddDays(30);

			_unitOfWork.OrderHeader.Update(order);
            _unitOfWork.Save();

            TempData["Success"] = "Order Shipped Successfully";

            return RedirectToAction(nameof(Details), new { orderId = OrderVm.OrderHeader.Id });
        }

		[HttpPost]
        [Authorize(Roles = StaticDetails.Role_Admin + "," + StaticDetails.Role_Employee)]
        public IActionResult CancelOrder()
		{
			var order = _unitOfWork.OrderHeader.Get(o => o.Id == OrderVm.OrderHeader.Id);

			//Payment approved - give a refund
			if (order.PaymentStatus == StaticDetails.PaymentStatusApproved)
			{
				var options = new RefundCreateOptions
				{
					Reason = RefundReasons.RequestedByCustomer,
					PaymentIntent = order.PaymentIntentId
				};
				var refundService = new RefundService();
				Refund refund = refundService.Create(options);

				_unitOfWork.OrderHeader.UpdateStatus(order.Id, StaticDetails.StatusCancelled, StaticDetails.StatusRefunded);
			}
			else
				_unitOfWork.OrderHeader.UpdateStatus(order.Id, StaticDetails.StatusCancelled, StaticDetails.StatusCancelled);

            _unitOfWork.OrderHeader.Update(order);
            _unitOfWork.Save();

            TempData["Success"] = "Order Cancelled Successfully";

            return RedirectToAction(nameof(Details), new { orderId = OrderVm.OrderHeader.Id });
        }
        public IActionResult PaymentConfirmation(int orderId)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(oh => oh.Id == orderId, includeProperties: "ApplicationUser");
            if (orderHeader.PaymentStatus == StaticDetails.PaymentStatusDelayedPayment)
            {
                //this is a company order
                var stripeService = new SessionService();
                var paymentSession = stripeService.Get(orderHeader.SessionId);
                if (paymentSession.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(orderHeader.Id, paymentSession.Id, paymentSession.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, orderHeader.OrderStatus, StaticDetails.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }
            return View(orderId);
        }

        [ActionName("Details")]
		[HttpPost]
		public IActionResult DetailsPayNow()
		{
			OrderVm.OrderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == OrderVm.OrderHeader.Id, includeProperties: "ApplicationUser");
			OrderVm.OrderDetail = _unitOfWork.OrderDetail.GetAll(d => d.OrderHeaderId == OrderVm.OrderHeader.Id, includeProperties: "Product");


            var stripeSessionOptions = ConfigureStripeSessionOptions();

            SetCartItemsToStripePaymentSession(ref stripeSessionOptions);

            var service = new SessionService();
            Session session = service.Create(stripeSessionOptions);

            _unitOfWork.OrderHeader.UpdateStripePaymentId(OrderVm.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);

        }

        private SessionCreateOptions ConfigureStripeSessionOptions()
        {
            var domain = "https://localhost:44368/";
            return new SessionCreateOptions
            {
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderId={OrderVm.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/details?orderId={OrderVm.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };
        }
        private void SetCartItemsToStripePaymentSession(ref SessionCreateOptions stripeSessionOptions)
        {
            foreach (var item in OrderVm.OrderDetail)
            {
                var sessionItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions()
                    {
                        UnitAmount = (long)(item.Price * 100), //$20.50 => 2050
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions()
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                };
                stripeSessionOptions.LineItems.Add(sessionItem);
            }
        }

        #region API CALLS
        [HttpGet]
		public IActionResult GetAll(string status)
		{
			IEnumerable<OrderHeader> orders;

			if (User.IsInRole(StaticDetails.Role_Admin) || User.IsInRole(StaticDetails.Role_Employee))
				orders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();

			else
			{
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

				orders = _unitOfWork.OrderHeader.GetAll(o => o.ApplicationUserId == userId, includeProperties: "ApplicationUser");
			}

			switch (status)
			{
				case "inprocess":
					orders = orders.Where(o => o.OrderStatus == StaticDetails.StatusInProcess);
					break;
				case "pending":
					orders = orders.Where(o => o.OrderStatus == StaticDetails.StatusPending);
					break;
				case "completed":
                    orders = orders.Where(o => o.OrderStatus == StaticDetails.StatusShipped);
                    break;
				case "approved":
                    orders = orders.Where(o => o.OrderStatus == StaticDetails.StatusApproved);
                    break;
                default:
					break;
			}

			return Json(new { data = orders });
		}
		#endregion
	}
}
