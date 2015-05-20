using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Hermes.Storage
{
	public interface IRepository<T>
		 where T : StorageObject
    {
		/// <exception cref="RepositoryException">RepositoryException</exception>
        IEnumerable<T> GetAll(Expression<Func<T, bool>> predicate = null);

		/// <exception cref="RepositoryException">RepositoryException</exception>
		T Get (string id);

		/// <exception cref="RepositoryException">RepositoryException</exception>
        T Get(Expression<Func<T, bool>> predicate);

		/// <exception cref="RepositoryException">RepositoryException</exception>
        bool Exist(string id);

		/// <exception cref="RepositoryException">RepositoryException</exception>
        bool Exist(Expression<Func<T, bool>> predicate);

		/// <exception cref="RepositoryException">RepositoryException</exception>
        void Create(T element);

		/// <exception cref="RepositoryException">RepositoryException</exception>
        void Update(T element);

		/// <exception cref="RepositoryException">RepositoryException</exception>
        void Delete(T element);

		/// <exception cref="RepositoryException">RepositoryException</exception>
		void Delete (Expression<Func<T, bool>> predicate);
    }
}
