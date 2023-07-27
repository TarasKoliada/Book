﻿using BookWeb.DataAccess.Data;
using BookWeb.DataAccess.Repository.IRepository;

namespace BookWeb.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        public ICategoryRepository Category { get; private set; }
        public IProductRepository Product { get; private set; }

        public ICompanyRepository Company { get; private set; }

        public IShoppingCartRepository ShoppingCart { get; private set; }
        public UnitOfWork(ApplicationDbContext context) 
        {
            _context = context; 
            Category = new CategoryRepository(context);
            Product = new ProductRepository(context);
            Company = new CompanyRepository(context);
            ShoppingCart = new ShoppingCartRepository(context);
        }
        

        public void Save() => _context.SaveChanges();
    }
}
