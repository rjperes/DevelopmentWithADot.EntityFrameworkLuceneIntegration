using System;
using System.Data.Entity;
using System.Linq;
using System.Reflection;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegration
{
	public static class QueryableExtensions
	{
		public static void DeleteIndex<T>(this DbSet<T> query) where T : class
		{
			Object internalContext = (query as IQueryable<T>).Provider.GetType().GetField("_internalContext", BindingFlags.NonPublic | BindingFlags.Instance).GetValue((query as IQueryable<T>).Provider);
			IndexedDbContext context = internalContext.GetType().BaseType.GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(internalContext) as IndexedDbContext;

			if (context != null)
			{
				context.DeleteIndex<T>();
			}
		}

		public static void Index<T>(this IQueryable<T> query, Boolean destroyExistingIndex = false) where T : class
		{
			Object internalContext = query.Provider.GetType().GetField("_internalContext", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(query.Provider);
			IndexedDbContext context = internalContext.GetType().BaseType.GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(internalContext) as IndexedDbContext;

			if (context != null)
			{
				foreach (T entity in query.ToList())
				{
					context.Index(entity, destroyExistingIndex);
				}
			}
		}
	}
}
