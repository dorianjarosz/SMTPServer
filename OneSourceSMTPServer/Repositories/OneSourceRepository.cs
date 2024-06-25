using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OneSourceSMTPServer.Data;

namespace OneSourceSMTPServer.Repositories
{
    public class OneSourceRepository : IOneSourceRepository
    {
        private readonly IDbContextFactory<OneSourceContext> _contextFactory;

        public OneSourceRepository(IDbContextFactory<OneSourceContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task MigrateDatabaseAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            await context.Database.MigrateAsync();
        }

        public async Task AddAsync<TEntity>(TEntity entity) where TEntity : class
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            context.Set<TEntity>().Add(entity);
            await context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<TEntity>> GetAllAsync<TEntity>() where TEntity : class
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Set<TEntity>().ToListAsync();
        }

        public async Task<IReadOnlyList<TEntity>> GetAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Set<TEntity>().Where(predicate).ToListAsync();
        }

        public async Task<TEntity> GetOneAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.Set<TEntity>().FirstOrDefaultAsync(predicate);

            return entity;
        }

        public async Task RemoveRangeAsync<TEntity>(IReadOnlyList<TEntity> entities) where TEntity : class
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            context.Set<TEntity>().RemoveRange(entities);

            await context.SaveChangesAsync();
        }
    }
}
