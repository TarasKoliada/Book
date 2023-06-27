using BookWeb.DataAccess.Repository.IRepository;
using BookWeb.Models;
using BookWeb.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork; 
            _webHostEnvironment = webHostEnvironment;
        }


        public IActionResult Index()
        {
            
            return View(_unitOfWork.Product.GetAll(includeProperties: "Category").ToList());
        }

        public IActionResult Upsert(int? id)
        {
            ProductVM product = new()
            {
                //Convert every category item to SelectListItem only with 2 fields - Name and Id
                CategoryList = _unitOfWork.Category.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                }),
                Product = id == null ? new Product() : _unitOfWork.Product.Get(p => p.Id == id)
            };
            return View(product);
        }

        [HttpPost]
        public IActionResult Upsert(ProductVM productVM, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                ProcessProductImage(ref productVM, file);
                if (productVM.Product.Id == 0) _unitOfWork.Product.Add(productVM.Product);
                else _unitOfWork.Product.Update(productVM.Product);

                _unitOfWork.Save();
                TempData["success"] = "Product created successfully";
                return RedirectToAction("Index", "Product");
            }
            else 
            {
                productVM.CategoryList = _unitOfWork.Category.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                });
                return View(productVM);
            }
        }
        private void ProcessProductImage(ref ProductVM productVM, IFormFile? file)
        {
            var wwwRootPath = _webHostEnvironment.WebRootPath;
            if (file != null)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(wwwRootPath, @"images\product");

                if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                {
                    var oldImgPath = Path.Combine(wwwRootPath, productVM.Product.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImgPath))
                    {
                        System.IO.File.Delete(oldImgPath);
                    }
                }

                using var fileStream = new FileStream(Path.Combine(filePath, fileName), FileMode.Create);
                file.CopyTo(fileStream);

                productVM.Product.ImageUrl = @"\images\product\" + fileName;
            }
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var products = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = products });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productToDelete = _unitOfWork.Product.Get(p => p.Id == id);
            if(productToDelete == null) return Json(new { success = false, message = "Error while deleting"});

            var oldProductImg = Path.Combine(_webHostEnvironment.WebRootPath, productToDelete.ImageUrl.TrimStart('\\'));

            if (System.IO.File.Exists(oldProductImg))
                System.IO.File.Delete(oldProductImg);

            _unitOfWork.Product.Remove(productToDelete);
            _unitOfWork.Save();
            var products = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { success = true, message = "Delete successfull"});
        }
        #endregion
    }
}
