using Commerce.DataAccess.Data;
using Commerce.DataAccess.Repository.IRepository;
using Commerce.Models;

namespace Commerce.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context) : base(context) => _context = context;

        public void Update(Product product)
        {
            var productFromDb = _context.Products.FirstOrDefault(p => p.Id == product.Id);
            if (productFromDb != null)
            {
                productFromDb.Title = product.Title;
                productFromDb.ISBN = product.ISBN;
                productFromDb.Price = product.Price;
                productFromDb.Price50 = product.Price50;
                productFromDb.ListPrice = product.ListPrice;
                productFromDb.Price100 = product.Price100;
                productFromDb.Description = product.Description;
                productFromDb.CategoryId = product.CategoryId;
                productFromDb.Author = product.Author;                
                productFromDb.ProductImages = product.ProductImages;
            }
        }
    }
}
