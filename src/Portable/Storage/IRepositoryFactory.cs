namespace Hermes.Storage
{
	public interface IRepositoryFactory
	{
		IRepository<T> CreateRepository<T> ();
	}
}
