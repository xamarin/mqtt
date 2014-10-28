using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Hermes.Storage
{
	public class InMemoryRepository<T> : IRepository<T>
    {
        readonly IList<T> elements;

        public InMemoryRepository()
        {
            this.elements = new List<T>();
        }

		/// <exception cref="RepositoryException">RepositoryException</exception>
        public IQueryable<T> GetAll(Expression<Func<T, bool>> predicate = null)
        {
            IEnumerable<T> result = this.elements;

            if (predicate != null)
            {
                result = result.Where(predicate.Compile());
            }

            return result.AsQueryable();
        }

		/// <exception cref="RepositoryException">RepositoryException</exception>
        public T Get(Expression<Func<T, bool>> predicate = null)
        {
			return this.elements.FirstOrDefault (predicate.Compile ());
        }

		/// <exception cref="RepositoryException">RepositoryException</exception>
        public bool Exist(Expression<Func<T, bool>> predicate = null)
        {
            var existingElement = this.Get(predicate);

            return existingElement != null;
        }

		/// <exception cref="RepositoryException">RepositoryException</exception>
        public void Create(T element)
        {
            this.elements.Add(element);
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
            this.elements.Remove(element);
        }

		/// <exception cref="RepositoryException">RepositoryException</exception>
		public void Delete(Expression<Func<T, bool>> predicate = null)
        {
			var element = this.Get (predicate);

			this.Delete (element);
        }
    }
}
