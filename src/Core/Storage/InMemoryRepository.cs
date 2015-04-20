using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Hermes.Storage
{
	public class InMemoryRepository<T> : IRepository<T>
		where T : StorageObject
    {
        static readonly ConcurrentDictionary<string, T> elements;

		static InMemoryRepository()
		{
			elements = new ConcurrentDictionary<string, T>();
		}

		/// <exception cref="RepositoryException">RepositoryException</exception>
        public IQueryable<T> GetAll(Expression<Func<T, bool>> predicate = null)
        {
            IEnumerable<T> result = elements.Select(x => x.Value);

            if (predicate != null)
            {
                result = result.Where(predicate.Compile());
            }

            return result.AsQueryable();
        }

		/// <exception cref="RepositoryException">RepositoryException</exception>
        public T Get(string id)
        {
			var element = default (T);

			elements.TryGetValue (id, out element);

			return element;
        }

		/// <exception cref="RepositoryException">RepositoryException</exception>
        public T Get(Expression<Func<T, bool>> predicate)
        {
			return elements.Select(x => x.Value).FirstOrDefault (predicate.Compile ());
        }

		/// <exception cref="RepositoryException">RepositoryException</exception>
        public bool Exist(string id)
        {
            return elements.Select(x => x.Value).Any (x => x.Id == id);
        }

		/// <exception cref="RepositoryException">RepositoryException</exception>
        public bool Exist(Expression<Func<T, bool>> predicate)
        {
            return elements.Select(x => x.Value).Any (predicate.Compile ());
        }

		/// <exception cref="RepositoryException">RepositoryException</exception>
        public void Create(T element)
        {
			elements.TryAdd (element.Id, element);
        }

		/// <exception cref="RepositoryException">RepositoryException</exception>
        public void Update(T element)
        {
            this.Delete(element);
            this.Create(element);
        }

		/// <exception cref="RepositoryException">RepositoryException</exception>
        public void Delete(T element)
        {
			var removedElement = default(T);

			elements.TryRemove (element.Id, out removedElement);
        }

		/// <exception cref="RepositoryException">RepositoryException</exception>
		public void Delete(Expression<Func<T, bool>> predicate)
        {
			var element = this.Get (predicate);

			if (element == null) {
				return;
			}

			this.Delete (element);
        }
    }
}
