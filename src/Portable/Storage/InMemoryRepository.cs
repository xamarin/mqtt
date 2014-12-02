using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Hermes.Storage
{
	//TODO: Add exception handling to prevent any repository error
	public class InMemoryRepository<T> : IRepository<T>
    {
        static readonly IList<T> elements;

		static InMemoryRepository()
		{
			elements = new List<T>();
		}

		/// <exception cref="RepositoryException">RepositoryException</exception>
        public IQueryable<T> GetAll(Expression<Func<T, bool>> predicate = null)
        {
            IEnumerable<T> result = elements;

            if (predicate != null)
            {
                result = result.Where(predicate.Compile());
            }

            return result.AsQueryable();
        }

		/// <exception cref="RepositoryException">RepositoryException</exception>
        public T Get(Expression<Func<T, bool>> predicate)
        {
			return elements.FirstOrDefault (predicate.Compile ());
        }

		/// <exception cref="RepositoryException">RepositoryException</exception>
        public bool Exist(Expression<Func<T, bool>> predicate)
        {
            var existingElement = this.Get(predicate);

            return existingElement != null;
        }

		/// <exception cref="RepositoryException">RepositoryException</exception>
        public void Create(T element)
        {
            elements.Add(element);
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
            elements.Remove(element);
        }

		/// <exception cref="RepositoryException">RepositoryException</exception>
		public void Delete(Expression<Func<T, bool>> predicate)
        {
			var element = this.Get (predicate);

			this.Delete (element);
        }
    }
}
