using System.Collections.Generic;
using System.Linq;

namespace System.Net.Mqtt.Storage
{
	internal class InMemoryRepositoryProvider : IRepositoryProvider
	{
		readonly IDictionary<Type, object> repositories;

		public InMemoryRepositoryProvider ()
		{
			repositories = new Dictionary<Type, object> ();
		}

		public IRepository<T> GetRepository<T> ()
			where T : StorageObject
		{
			if (repositories.Any (r => r.Key == typeof (T))) {
				return repositories.FirstOrDefault (r => r.Key == typeof (T)).Value as IRepository<T>;
			}

			var repository = new InMemoryRepository<T> ();

			repositories.Add (typeof (T), repository);

			return repository;
		}
	}
}
