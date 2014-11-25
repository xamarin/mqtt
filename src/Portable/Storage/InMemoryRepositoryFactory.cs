namespace Hermes.Storage
{
	public class InMemoryRepositoryFactory : IRepositoryFactory
	{
		public IRepository<T> CreateRepository<T> ()
		{
			return new InMemoryRepository<T> ();
		}
	}
}
