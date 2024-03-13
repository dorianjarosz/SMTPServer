using System.Linq.Expressions;

namespace SMTPServer.Repositories
{
    public interface IOneSourceRepository
    {
        Task<IReadOnlyList<TEntity>> GetAllAsync<TEntity>() where TEntity : class;

        Task<IReadOnlyList<TEntity>> GetAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class;

        Task AddAsync<TEntity>(TEntity entity) where TEntity : class;

        Task RemoveRangeAsync<TEntity>(IReadOnlyList<TEntity> entities) where TEntity : class;
    }
}
