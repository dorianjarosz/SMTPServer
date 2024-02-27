﻿using Microsoft.EntityFrameworkCore;
using SMTPReceiver.Data;

namespace SMTPReceiver.Repositories
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
    }
}
