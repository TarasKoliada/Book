using BookWeb.DataAccess.Data;
using BookWeb.Models;

namespace BookWeb.DataAccess.Repository.IRepository
{
    public class ApplicationUserRepository : Repository<ApplicationUser>, IApplicationUserRepository
    {
        public ApplicationUserRepository(ApplicationDbContext context) : base(context)
        { }
    }
}
