using System;
using System.Collections.Generic;
using System.Linq;

namespace Hermes.Storage
{
	public class InMemoryRepositoryFactory : IRepositoryFactory
	{
		readonly IDictionary<Type, object> repositories;

		public InMemoryRepositoryFactory ()
		{
			this.repositories = new Dictionary<Type, object> ();
		}

		public IRepository<T> CreateRepository<T> ()
		{
			if (this.repositories.Any (r => r.Key == typeof (T))) {
				return this.repositories.FirstOrDefault (r => r.Key == typeof (T)).Value as IRepository<T>;
			}

			var repository = new InMemoryRepository<T> ();

			this.repositories.Add (typeof (T), repository);

			return repository;
		}
	}
}
