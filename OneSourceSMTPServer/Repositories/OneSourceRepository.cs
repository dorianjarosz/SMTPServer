using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OneSourceSMTPServer.Data;

namespace OneSourceSMTPServer.Repositories
{
    public class OneSourceRepository : IOneSourceRepository
    {
        private readonly OneSourceContext _oneSourceContext;

        public OneSourceRepository(OneSourceContext oneSourceContext)
        {
            _oneSourceContext = oneSourceContext;
        }

        public async Task AddAsync<TEntity>(TEntity entity) where TEntity : class
        {
            _oneSourceContext.Set<TEntity>().Add(entity);
            await _oneSourceContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<TEntity>> GetAllAsync<TEntity>() where TEntity : class
        {
            return await _oneSourceContext.Set<TEntity>().ToListAsync();
        }

        public async Task<IReadOnlyList<TEntity>> GetAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            return await _oneSourceContext.Set<TEntity>().Where(predicate).ToListAsync();
        }

        public async Task RemoveRangeAsync<TEntity>(IReadOnlyList<TEntity> entities) where TEntity : class
        {
            _oneSourceContext.Set<TEntity>().RemoveRange(entities);

            await _oneSourceContext.SaveChangesAsync();
        }
    }
}
