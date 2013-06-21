using System;
using System.Linq;
using System.Linq.Expressions;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegration
{
	public interface ILuceneContext
	{
		void Index(Object entity, Boolean destroyExistingIndex = false);
		void DeleteIndex<T>();
		IQueryable<T> Search<T>(Expression<Func<T, Boolean>> condition, Int32 maxResults = 0) where T : class;
		IQueryable<T> Search<T>(String queryString, Int32 maxResults = 0) where T : class;
		IQueryable<T> Search<T>(String name, Object value, Int32 maxResults = 0) where T : class;
	}
}
