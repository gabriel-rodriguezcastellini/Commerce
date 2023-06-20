using Commerce.DataAccess.Data;
using Commerce.DataAccess.Repository.IRepository;
using Commerce.Models;

namespace Commerce.DataAccess.Repository
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoryRepository(ApplicationDbContext context) : base(context) => _context = context;

        public void Update(Category category) => _context.Categories.Update(category);
    }
}
