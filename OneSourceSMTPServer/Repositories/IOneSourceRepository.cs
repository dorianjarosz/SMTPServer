﻿using System.Linq.Expressions;

namespace OneSourceSMTPServer.Repositories
{
    public interface IOneSourceRepository
    {
        Task MigrateDatabaseAsync();

        Task<IReadOnlyList<TEntity>> GetAllAsync<TEntity>() where TEntity : class;

        Task<IReadOnlyList<TEntity>> GetAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class;

        Task<TEntity> GetOneAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class;

        Task AddAsync<TEntity>(TEntity entity) where TEntity : class;

        Task RemoveRangeAsync<TEntity>(IReadOnlyList<TEntity> entities) where TEntity : class;
    }
}
