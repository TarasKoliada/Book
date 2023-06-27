using BookWeb.DataAccess.Data;
using BookWeb.DataAccess.Repository.IRepository;
using BookWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookWeb.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _context;
        public ProductRepository(ApplicationDbContext context) : base(context) { _context = context; }
        public void Update(Product product)
        {
            var dbEntity = _context.Products.FirstOrDefault(p => p.Id == product.Id);
            if (dbEntity != null)
            {
                dbEntity.Title = product.Title;
                dbEntity.ISBN = product.ISBN;
                dbEntity.ListPrice = product.ListPrice;
                dbEntity.Price = product.Price;
                dbEntity.Description = product.Description;
                dbEntity.Price50 = product.Price50;
                dbEntity.Price100 = product.Price100;
                dbEntity.Author = product.Author;
                dbEntity.CategoryId = product.CategoryId;

                if (product.ImageUrl != null)
                    dbEntity.ImageUrl = product.ImageUrl;
            }
        }
    }
}
