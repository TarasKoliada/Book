using BookWeb.DataAccess.Repository.IRepository;
using BookWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public ProductController(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }


        public IActionResult Index()
        {
            
            return View(_unitOfWork.Product.GetAll().ToList());
        }

        public IActionResult Create()
        {
            //Convert every category item to SelectListItem only with 2 fields - Name and Id
            IEnumerable<SelectListItem> categoryList = _unitOfWork.Category.GetAll().Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            });
            //ViewBag - dynamic property, transfers data from controller to view and its life lasts during the current http request
            ViewBag.CategoryList = categoryList;
            return View();
        }

        [HttpPost]
        public IActionResult Create(Product product)
        {
            if (ModelState.IsValid) 
            {
                _unitOfWork.Product.Add(product);
                _unitOfWork.Save();
                TempData["success"] = "Product created successfully";
                return RedirectToAction("Index", "Product");
            }
            return View();
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
                return NotFound();

            var productToEdit = _unitOfWork.Product.Get(p => p.Id == id);
            if (productToEdit == null)
                return NotFound();

            return View(productToEdit);
        }

        [HttpPost]
        public IActionResult Edit(Product product)
        {
            //if modelState is valid - update, else return the same Edit view till modelstate become valid
            if (ModelState.IsValid)
            {
                _unitOfWork.Product.Update(product);
                _unitOfWork.Save();
                TempData["success"] = "Product updated successfully";
                return RedirectToAction("Index", "Product");
            }
            return View();
        }

        public IActionResult Delete(int? id) 
        {
            if(id == null || id <= 0) return NotFound();

            var productToDelete = _unitOfWork.Product.Get(p => p.Id == id);

            if(productToDelete == null) return NotFound();
            return View(productToDelete);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteProduct(int? id)
        {
            var productToDelete = _unitOfWork.Product.Get(p => p.Id == id);
            if (productToDelete == null) return NotFound();

            _unitOfWork.Product.Remove(productToDelete);
            _unitOfWork.Save();

            TempData["success"] = "Product deleted successfully";
            return RedirectToAction("Index", "Product");
        }
    }
}
