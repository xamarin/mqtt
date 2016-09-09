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

		public IEnumerable<T> GetAll (Expression<Func<T, bool>> predicate = null)
		{
			var result = elements.Select(x => x.Value);

			if (predicate != null) {
				result = result.Where (predicate.Compile ());
			}

			return result;
		}

		public T Get (string id)
		{
			var element = default (T);

			elements.TryGetValue (id, out element);

			return element;
		}

		public T Get (Expression<Func<T, bool>> predicate)
		{
			return GetAll ().FirstOrDefault (predicate.Compile ());
		}

		public bool Exist (string id)
		{
			return Get (id) != default (T);
		}

		public bool Exist (Expression<Func<T, bool>> predicate)
		{
			return Get (predicate) != default (T);
		}

		public void Create (T element)
		{
			elements.TryAdd (element.Id, element);
		}

		public void Update (T element)
		{
			Delete (element);
			Create (element);
		}

		public void Delete (T element)
		{
			var removedElement = default(T);

			elements.TryRemove (element.Id, out removedElement);
		}

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
