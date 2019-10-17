using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kraken.Caching.Reflection
{
	internal static class TypeExtensions
	{
		internal static IEnumerable<Type> BrowseTypes(this Type type)
		{
			IList<Type> processed = new List<Type>();
			Queue<Type> queue = new Queue<Type>();

			queue.Enqueue(type);
			while (queue.Count > 0)
			{
				Type current = queue.Dequeue();
				processed.Add(current);
				yield return current;
#if NETSTANDARD1_6
				Type baseType = current.GetTypeInfo().BaseType;
				if (baseType != null && baseType != typeof(object))
					queue.Enqueue(current.GetTypeInfo().BaseType);
				foreach (Type intf in current.GetTypeInfo().GetInterfaces())
					if (!processed.Contains(intf))
						queue.Enqueue(intf);
#else
				if (current.BaseType != null && current.BaseType != typeof(object))
					queue.Enqueue(current.BaseType);
				foreach (Type intf in current.GetInterfaces())
					if (!processed.Contains(intf))
						queue.Enqueue(intf);
#endif
			}
		}

		internal static MethodInfo GetLocalMethod(this Type type, MethodInfo method)
		{
			ParameterInfo[] pars = method.GetParameters();
			ParameterModifier mod = pars.Length > 0 ? new ParameterModifier(pars.Length) : new ParameterModifier();
			for (int i = 0; i < pars.Length; i++)
			{
				ParameterInfo par = pars[i];
				if (par.ParameterType.IsByRef)
				{
					mod[i] = true;
				}
			}
			MethodInfo result = type
#if NETSTANDARD1_6
				.GetTypeInfo()
#endif
				.GetMethod(method.Name,
#if NETSTANDARD2_1
				method.GetGenericArguments().Length,
#endif
				pars.Select(o => o.ParameterType).ToArray(),
				pars.Length > 0 ? new[] { mod } : null
				);
			return result;
		}

		internal static string GetFieldName(this Type t)
		{
			string name = t.Name;
			if (t
#if NETSTANDARD1_6
				.GetTypeInfo()
#endif
				.IsInterface && name[0] == 'I')
			{
				name = name.Substring(1);
			}
			return name.Substring(0, 1).ToLowerInvariant() + name.Substring(1);
		}
	}
}
