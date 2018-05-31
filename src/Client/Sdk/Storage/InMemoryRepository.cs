using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace System.Net.Mqtt.Sdk.Storage
{
	internal class InMemoryRepository<T> : IRepository<T>
		where T : IStorageObject
	{
		readonly ConcurrentDictionary<string, T> elements = new ConcurrentDictionary<string, T> ();

		public IEnumerable<T> ReadAll () => elements.Select (e => e.Value);

		public T Read (string id)
		{
			elements.TryGetValue (id, out T element);

			return element;
		}

		public void Create (T element) => elements.TryAdd (element.Id, element);

		public void Update (T element) => elements.AddOrUpdate (element.Id, element, (key, value) => element);

		public void Delete (string id) => elements.TryRemove (id, out T removedElement);
	}
}
