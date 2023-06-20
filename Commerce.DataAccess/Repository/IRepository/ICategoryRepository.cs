using Commerce.Models;

namespace Commerce.DataAccess.Repository.IRepository
{
    public interface ICategoryRepository : IRepository<Category>
    {
        void Update(Category category);        
    }
}
