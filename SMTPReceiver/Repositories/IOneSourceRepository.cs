namespace SMTPReceiver.Repositories
{
    public interface IOneSourceRepository
    {
        Task AddAsync<TEntity>(TEntity entity) where TEntity : class;
    }
}
