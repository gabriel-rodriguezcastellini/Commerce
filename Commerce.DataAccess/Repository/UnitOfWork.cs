using Commerce.DataAccess.Data;
using Commerce.DataAccess.Repository.IRepository;

namespace Commerce.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        public ICategoryRepository CategoryRepository { get; private set; }
        public IProductRepository ProductRepository { get; private set; }
        public ICompanyRepository CompanyRepository { get; private set; }
        public IShoppingCartRepository ShoppingCartRepository { get; private set; }
        public IApplicationUserRepository ApplicationUserRepository { get; private set; }
        public IOrderHeaderRepository OrderHeaderRepository { get; private set; }
        public IOrderDetailRepository OrderDetailRepository { get; private set; }
        public IProductImageRepository ProductImageRepository { get; private set; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            CategoryRepository = new CategoryRepository(_context);
            ProductRepository = new ProductRepository(_context);
            CompanyRepository = new CompanyRepository(_context);
            ShoppingCartRepository = new ShoppingCartRepository(_context);
            ApplicationUserRepository = new ApplicationUserRepository(_context);
            OrderHeaderRepository = new OrderHeaderRepository(_context);
            OrderDetailRepository = new OrderDetailRepository(_context);
            ProductImageRepository = new ProductImageRepository(_context);
        }

        public void Save() => _context.SaveChanges();
    }
}
