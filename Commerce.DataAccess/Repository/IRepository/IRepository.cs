using System.Linq.Expressions;

namespace Commerce.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        IEnumerable<T> GetAll(Expression<Func<T, bool>>? expression = null, string? includeProperties = null);
        T Get(Expression<Func<T, bool>> expression, string? includeProperties = null, bool track = false);
        void Add(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);

    }
}
