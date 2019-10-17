using Kraken.Caching.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Kraken.Caching
{
	public static class ServiceExtensions
	{
		public static TResult CachedLocal<TSource, TResult>(this IServiceProvider serviceProvider, TSource instance, Expression<Func<TSource, TResult>> expression)
			where TSource : class
		{
			MethodInfo methodInfo = GetMethod(instance, expression, out object[] par);

			IEnumerable<Type> types = instance.GetType().BrowseTypes()
				.OrderBy(o => o
#if NETSTANDARD1_6
					.GetTypeInfo()
#endif
					.IsInterface);
			foreach (Type type in types)
			{
				object svc = serviceProvider.GetService(type);
				if (svc != null && svc is CachedServiceBase)
				{
					MethodInfo local = svc.GetType().GetLocalMethod(methodInfo);
					if (local != null)
						return (TResult)local.Invoke(svc, par);
				}
			}

			return expression.Compile().Invoke(instance);
		}

		private static MethodInfo GetMethod<TSource, TResult>(TSource instance, Expression<Func<TSource, TResult>> expression, out object[] par)
			where TSource : class
		{
			Queue<Expression> queue = new Queue<Expression>();
			queue.Enqueue(expression);
			ParameterExpression[] paramList = null;
			while (queue.Count > 0)
			{
				Expression current = queue.Dequeue();
				if (current is LambdaExpression lex)
				{
					queue.Enqueue(lex.Body);
					paramList = lex.Parameters.ToArray();
				}
				else if (current is UnaryExpression uex)
					queue.Enqueue(uex.Operand);
				else if (current is BinaryExpression bex)
				{
					queue.Enqueue(bex.Left);
					queue.Enqueue(bex.Right);
				}
				else if (current is MethodCallExpression mce)
				{
					par = TranslateArguments(instance, paramList, mce.Arguments).ToArray();
					return mce.Method;
				}
			}
			par = new object[0];
			return null;
		}

		private static IEnumerable<object> TranslateArguments<TSource>(TSource instance, IEnumerable<ParameterExpression> paramList, IEnumerable<Expression> arguments)
		{
			foreach (Expression arg in arguments)
			{
				Func<TSource, object> expr = Expression.Lambda<Func<TSource, object>>(arg, paramList).Compile();
				object item = expr(instance);
				yield return item;
			}
		}
	}
}
