namespace SMTPReceiver.Repositories
{
    public interface IOneSourceRepository
    {
        Task<IReadOnlyList<TEntity>> GetAllAsync<TEntity>() where TEntity : class;

        Task AddAsync<TEntity>(TEntity entity) where TEntity : class;
    }
}
