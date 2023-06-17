using BookWeb.DataAccess.Data;
using BookWeb.DataAccess.Repository.IRepository;
using BookWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookWeb.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepo;
        public CategoryController(ICategoryRepository repository)
        {
            _categoryRepo = repository;
        }
        public IActionResult Index()
        {
            var categories = _categoryRepo.GetAll().ToList();
            return View(categories);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category category)
        {
            //Check if category model pass all validations initialized in Category.cs
            if(ModelState.IsValid)
            { 
                _categoryRepo.Add(category);
                _categoryRepo.Save();
                TempData["success"] = "Category created successfully";
                return RedirectToAction("Index", "Category");
            }
            return View();
        }

        public IActionResult Edit(int? id) 
        {
            if(id == null || id == 0)
                return NotFound();

            var categoryToEdit = _categoryRepo.Get(c => c.Id == id);
            if (categoryToEdit == null)
                return NotFound();

            return View(categoryToEdit);
        }

        [HttpPost]
        public IActionResult Edit(Category category) 
        {
            if (ModelState.IsValid) 
            {
                _categoryRepo.Update(category);
                _categoryRepo.Save();
                TempData["success"] = "Category updated successfully";
                return RedirectToAction("Index", "Category");
            }
            return View();
        }

        public IActionResult Delete(int? id) 
        {
            if (id == null || id == 0)
                return NotFound();

            var categoryToDelete = _categoryRepo.Get(c => c.Id == id);
            if (categoryToDelete == null)
                return NotFound();

            return View(categoryToDelete);
        }
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteCategory(int? id)
        {
            var categoryToDelete = _categoryRepo.Get(c => c.Id == id);
            if (categoryToDelete == null)
                return NotFound(categoryToDelete);

            _categoryRepo.Remove(categoryToDelete);
            _categoryRepo.Save();
            TempData["success"] = "Category deleted successfully";
            return RedirectToAction("Index", "Category");
        }
    }
}
