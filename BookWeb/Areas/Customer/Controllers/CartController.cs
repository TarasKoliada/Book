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
        public ShoppingCartVM ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            { 
                ShoppingCartsList = _unitOfWork.ShoppingCart.GetAll(sc => sc.UserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };

            foreach (var cart in ShoppingCartVM.ShoppingCartsList)
            {
                cart.ItemPrice = GetPriceBasedOnOrderedQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += cart.ItemPrice * cart.Count;
            }

            return View(ShoppingCartVM);
        }

        public IActionResult Summary()
        {
            return View();
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
    }
}
