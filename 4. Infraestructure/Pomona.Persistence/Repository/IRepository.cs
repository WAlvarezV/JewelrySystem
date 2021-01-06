namespace Pomona.Persistence.Repository
{
    internal interface IRepository<T> where T : class
    {
        T Insert(T entity);
    }
}
