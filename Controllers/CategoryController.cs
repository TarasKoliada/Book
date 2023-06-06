using BookWeb.Data;
using BookWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookWeb.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CategoryController(ApplicationDbContext dbContext)
        {
            _context = dbContext;
        }
        public IActionResult Index()
        {
            var categories = _context.Categories.ToList();
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
                _context.Categories.Add(category);
                _context.SaveChanges();
                return RedirectToAction("Index", "Category");
            }
            return View();
        }

        public IActionResult Edit(int? id) 
        {
            if(id == null || id == 0)
                return NotFound();

            var categoryToEdit = _context.Categories.FirstOrDefault(c => c.Id == id);
            if(categoryToEdit == null)
                return NotFound();

            return View(categoryToEdit);
        }

        [HttpPost]
        public IActionResult Edit(Category category) 
        {
            if (ModelState.IsValid) 
            {
                _context.Categories.Update(category);
                _context.SaveChanges();
                return RedirectToAction("Index", "Category");
            }
            return View();
        }
    }
}
