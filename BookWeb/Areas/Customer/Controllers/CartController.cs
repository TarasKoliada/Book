using BookWeb.DataAccess.Repository.IRepository;
using BookWeb.Models;
using BookWeb.Models.ViewModels;
using BookWeb.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace BookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;

            ShoppingCartVM = new()
            {
                ShoppingCartsList = _unitOfWork.ShoppingCart.GetAll(sc => sc.UserId == GetCurrentUserId(), includeProperties: "Product"),
                OrderHeader = new()
            };
        }
        public IActionResult Index()
        {
            ShoppingCartVM.OrderHeader.OrderTotal = CalculateCurrentUserOrderTotal();

            return View(ShoppingCartVM);
        }

        public IActionResult Summary()
        {
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == GetCurrentUserId());

            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            ShoppingCartVM.OrderHeader.OrderTotal = CalculateCurrentUserOrderTotal();

            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPOST()
        {
            var currentUserId = GetCurrentUserId();
            ShoppingCartVM.ShoppingCartsList = _unitOfWork.ShoppingCart.GetAll(sc => sc.UserId == currentUserId, includeProperties: "Product");

            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = currentUserId;
            ShoppingCartVM.OrderHeader.OrderTotal = CalculateCurrentUserOrderTotal();

            var applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == currentUserId);

            if (!User.IsInRole(StaticDetails.Role_Company))
            {
                ShoppingCartVM.OrderHeader.OrderStatus = StaticDetails.StatusPending;
                ShoppingCartVM.OrderHeader.PaymentStatus = StaticDetails.PaymentStatusPending;
            }
            else
            {
				ShoppingCartVM.OrderHeader.OrderStatus = StaticDetails.StatusApproved;
				ShoppingCartVM.OrderHeader.PaymentStatus = StaticDetails.PaymentStatusDelayedPayment;
			}

            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            foreach (var cart in ShoppingCartVM.ShoppingCartsList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Count = cart.Count,
                    Price = cart.ItemPrice
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }

            //If not Company Role - call Stripe payment system
            if (!User.IsInRole(StaticDetails.Role_Company))
            {
                var stripeSessionOptions = ConfigureStripeSessionOptions();

                SetCartItemsToStripePaymentSession(ref stripeSessionOptions);

				var service = new SessionService();
				Session session = service.Create(stripeSessionOptions);

                _unitOfWork.OrderHeader.UpdateStripePaymentId(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
			}

            return RedirectToAction(nameof(OrderConfirmation), new { orderId = ShoppingCartVM.OrderHeader.Id});
        }

        public IActionResult OrderConfirmation(int orderId) 
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(oh => oh.Id == orderId, includeProperties: "ApplicationUser");
            if (orderHeader.PaymentStatus != StaticDetails.PaymentStatusDelayedPayment)
            {
                //this is a customer order
                var stripeService = new SessionService();
                var paymentSession = stripeService.Get(orderHeader.SessionId);
                if (paymentSession.PaymentStatus.ToLower() == "paid")
                {
					_unitOfWork.OrderHeader.UpdateStripePaymentId(orderHeader.Id, paymentSession.Id, paymentSession.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, StaticDetails.StatusApproved, StaticDetails.PaymentStatusApproved);
                    _unitOfWork.Save();
				}
            }
            var shoppingCarts = _unitOfWork.ShoppingCart.GetAll(sc => sc.UserId == orderHeader.ApplicationUserId).ToList();
            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();

            return View(orderId);
        }
        public IActionResult IncreaseQuantity(int cartItemId)
        {
            var cartItem = _unitOfWork.ShoppingCart.Get(sc => sc.Id == cartItemId);
            cartItem.Count += 1;
            _unitOfWork.ShoppingCart.Update(cartItem);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult DecreaseQuantity(int cartItemId)
        {
            var cartItem = _unitOfWork.ShoppingCart.Get(sc => sc.Id == cartItemId);
            if (cartItem.Count <= 1)
            { 
                _unitOfWork.ShoppingCart.Remove(cartItem); 
            }
            else
            {
                cartItem.Count -= 1;
                _unitOfWork.ShoppingCart.Update(cartItem);
            }

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartItemId)
        {
            var cartItem = _unitOfWork.ShoppingCart.Get(sc => sc.Id == cartItemId);
            _unitOfWork.ShoppingCart.Remove(cartItem);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        private SessionCreateOptions ConfigureStripeSessionOptions()
        {
			var domain = "https://localhost:44368/";
			return new SessionCreateOptions
			{
				SuccessUrl = domain + $"customer/cart/OrderConfirmation?orderId={ShoppingCartVM.OrderHeader.Id}",
				CancelUrl = domain + "customer/cart/index",
				LineItems = new List<SessionLineItemOptions>(),
				Mode = "payment",
			};
		}
        private void SetCartItemsToStripePaymentSession(ref SessionCreateOptions stripeSessionOptions)
        {
			foreach (var item in ShoppingCartVM.ShoppingCartsList)
			{
				var sessionItem = new SessionLineItemOptions
				{
					PriceData = new SessionLineItemPriceDataOptions()
					{
						UnitAmount = (long)(item.ItemPrice * 100), //$20.50 => 2050
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

        private double GetPriceBasedOnOrderedQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart == null)
                return 0;

            if (shoppingCart.Count <= 50)
                return shoppingCart.Product.Price;

            else if (shoppingCart.Count <= 100)
                return shoppingCart.Product.Price50;

            else return shoppingCart.Product.Price100;
        }

        private string GetCurrentUserId() => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        private double CalculateCurrentUserOrderTotal()
        {
            double total = 0;
            foreach (var cart in ShoppingCartVM.ShoppingCartsList)
            {
                cart.ItemPrice = GetPriceBasedOnOrderedQuantity(cart);
                total += cart.ItemPrice * cart.Count;
            }
            return total;
        }
    }
}
