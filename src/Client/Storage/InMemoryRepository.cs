using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace System.Net.Mqtt.Storage
{
	internal class InMemoryRepository<T> : IRepository<T>
		where T : StorageObject
	{
		readonly ConcurrentDictionary<string, T> elements;

		public InMemoryRepository ()
		{
			elements = new ConcurrentDictionary<string, T> ();
		}

		/// <exception cref="RepositoryException">RepositoryException</exception>
		public IEnumerable<T> GetAll (Expression<Func<T, bool>> predicate = null)
		{
			var result = elements.Select(x => x.Value);

			if (predicate != null) {
				result = result.Where (predicate.Compile ());
			}

			return result;
		}

		/// <exception cref="RepositoryException">RepositoryException</exception>
		public T Get (string id)
		{
			var element = default (T);

			elements.TryGetValue (id, out element);

			return element;
		}

		/// <exception cref="RepositoryException">RepositoryException</exception>
		public T Get (Expression<Func<T, bool>> predicate)
		{
			return GetAll ().FirstOrDefault (predicate.Compile ());
		}

		/// <exception cref="RepositoryException">RepositoryException</exception>
		public bool Exist (string id)
		{
			return Get (id) != default (T);
		}

		/// <exception cref="RepositoryException">RepositoryException</exception>
		public bool Exist (Expression<Func<T, bool>> predicate)
		{
			return Get (predicate) != default (T);
		}

		/// <exception cref="RepositoryException">RepositoryException</exception>
		public void Create (T element)
		{
			elements.TryAdd (element.Id, element);
		}

		/// <exception cref="RepositoryException">RepositoryException</exception>
		public void Update (T element)
		{
			Delete (element);
			Create (element);
		}

		/// <exception cref="RepositoryException">RepositoryException</exception>
		public void Delete (T element)
		{
			var removedElement = default(T);

			elements.TryRemove (element.Id, out removedElement);
		}

		/// <exception cref="RepositoryException">RepositoryException</exception>
		public void Delete (Expression<Func<T, bool>> predicate)
		{
			var element = Get (predicate);

			if (element == null) {
				return;
			}

			Delete (element);
		}
	}
}
