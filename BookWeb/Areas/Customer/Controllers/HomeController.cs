using BookWeb.DataAccess.Repository.IRepository;
using BookWeb.Models;
using BookWeb.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BookWeb.Areas.Customer.Controllers
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

        public IActionResult Index()
        {
            return View(_unitOfWork.Product.GetAll(includeProperties: "Category"));
        }
            
        public IActionResult Details(int id) => View(new ShoppingCart 
        {
            Product = _unitOfWork.Product.Get(p => p.Id == id, includeProperties: "Category"), 
            ProductId = id, 
            Count = 1 
        });

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            shoppingCart.Id = 0;

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            var cartFromDb = _unitOfWork.ShoppingCart.Get(sc => sc.UserId == userId && sc.ProductId == shoppingCart.ProductId);
            if (cartFromDb != null) //shopping cart exist
            {
                cartFromDb.Count += shoppingCart.Count;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
                _unitOfWork.Save();
                TempData["success"] = "Cart updated successfully";
            }
            else //shopping cart didnt exist
            {
                shoppingCart.UserId = userId;
                _unitOfWork.ShoppingCart.Add(shoppingCart);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32(StaticDetails.SessionCart, _unitOfWork.ShoppingCart.GetAll(c => c.UserId == userId).Count());
                TempData["success"] = "Cart created successfully";
            }
            
            
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}