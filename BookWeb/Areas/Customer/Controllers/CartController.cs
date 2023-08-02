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
                ShoppingCartsList = _unitOfWork.ShoppingCart.GetAll(sc => sc.UserId == userId, includeProperties: "Product")
            };

            foreach (var cart in ShoppingCartVM.ShoppingCartsList)
            {
                cart.ItemPrice = GetPriceBasedOnOrderedQuantity(cart);
                ShoppingCartVM.OrderTotal += cart.ItemPrice * cart.Count;
            }

            return View(ShoppingCartVM);
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
