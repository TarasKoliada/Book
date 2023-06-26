﻿using BookWeb.DataAccess.Repository.IRepository;
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
        public ProductController(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }


        public IActionResult Index()
        {
            
            return View(_unitOfWork.Product.GetAll().ToList());
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
                _unitOfWork.Product.Add(productVM.Product);
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