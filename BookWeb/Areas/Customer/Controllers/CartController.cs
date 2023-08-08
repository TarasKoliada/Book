using BookWeb.DataAccess.Repository.IRepository;
using BookWeb.Models;
using BookWeb.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ShoppingCartVM ShoppingCartVM { get; set; }

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
