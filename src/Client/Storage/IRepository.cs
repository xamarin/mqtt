using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Net.Mqtt.Storage
{
	internal interface IRepository<T>
		 where T : StorageObject
	{
		IEnumerable<T> GetAll (Expression<Func<T, bool>> predicate = null);

		T Get (string id);

		T Get (Expression<Func<T, bool>> predicate);

		bool Exist (string id);

		bool Exist (Expression<Func<T, bool>> predicate);

		void Create (T element);

		void Update (T element);

		void Delete (T element);

		void Delete (Expression<Func<T, bool>> predicate);
	}
}
