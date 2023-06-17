using BookWeb.DataAccess.Data;
using BookWeb.DataAccess.Repository.IRepository;

namespace BookWeb.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        public ICategoryRepository Category { get; private set; }
        public UnitOfWork(ApplicationDbContext context) 
        {
            _context = context; 
            Category = new CategoryRepository(context);
        }
        

        public void Save() => _context.SaveChanges();
    }
}
