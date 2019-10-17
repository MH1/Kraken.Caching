using Kraken.Caching.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Kraken.Caching.Engine
{
	internal partial class Generator
	{
		private static PropertyInfo GetImplementation(Type svcType, Type impType, PropertyInfo property)
			=> impType
#if NETSTANDARD1_6
				.GetTypeInfo()
#endif
				.GetProperty(property.Name, property.PropertyType,
					property.GetIndexParameters().Select(o => o.ParameterType).ToArray());

		private static MethodInfo GetImplementation(Type svcType, Type impType, MethodInfo method)
			=> GetImplementations(svcType, impType, method).FirstOrDefault()
				?? throw new InvalidOperationException($"Unable to find the {method.Name} implementation in {impType.FullName}");

		private static IEnumerable<MethodInfo> GetInterfaces(Type svcType, Type impType, MethodInfo method)
		{
			IEnumerable<Type> list = svcType.BrowseTypes();

			foreach (Type type in list)
			{
#if NETSTANDARD1_6
				InterfaceMapping map = impType.GetTypeInfo().GetRuntimeInterfaceMap(type);
#else
				InterfaceMapping map = impType.GetInterfaceMap(type);
#endif
				int index = Array.IndexOf(map.InterfaceMethods, method);
				if (index >= 0)
					yield return map.InterfaceMethods[index];
			}
		}

		private static IEnumerable<MethodInfo> GetImplementations(Type svcType, Type impType, MethodInfo method)
		{
			IEnumerable<Type> list = svcType.BrowseTypes();

			foreach (Type type in list)
			{
#if NETSTANDARD1_6
				InterfaceMapping map = impType.GetTypeInfo().GetRuntimeInterfaceMap(type);
#else
				InterfaceMapping map = impType.GetInterfaceMap(type);
#endif
				int index = Array.IndexOf(map.InterfaceMethods, method);
				if (index >= 0)
					yield return map.TargetMethods[index];
			}
		}

		private static T FindAttribute<T>(Type svcType, Type impType, PropertyInfo property) where T : Attribute
		{
			PropertyInfo impProperty = GetImplementation(svcType, impType, property);
			return
				// method annotations
				impProperty.GetCustomAttribute<T>(true)
				?? property.GetCustomAttribute<T>(true)
#if NETSTANDARD1_6
				// class / interface annotations
				?? impProperty.DeclaringType?.GetTypeInfo().GetCustomAttribute<T>(true)
				?? impType.GetTypeInfo().GetCustomAttribute<T>(true)
				?? svcType.GetTypeInfo().GetCustomAttribute<T>(true)
				// module attributes
				?? impProperty.DeclaringType?.GetTypeInfo().Module.GetCustomAttribute<T>()
				?? impType.GetTypeInfo().Module.GetCustomAttribute<T>()
				?? svcType.GetTypeInfo().Module.GetCustomAttribute<T>()
				// assembly attributes
				?? impProperty.DeclaringType?.GetTypeInfo().Assembly.GetCustomAttribute<T>()
				?? impType.GetTypeInfo().Assembly.GetCustomAttribute<T>()
				?? svcType.GetTypeInfo().Assembly.GetCustomAttribute<T>();
#else
				// class / interface annotations
				?? impProperty.DeclaringType?.GetCustomAttribute<T>(true)
				?? impType.GetCustomAttribute<T>(true)
				?? svcType.GetCustomAttribute<T>(true)
				// module attributes
				?? impProperty.DeclaringType?.Module.GetCustomAttribute<T>()
				?? impType.Module.GetCustomAttribute<T>()
				?? svcType.Module.GetCustomAttribute<T>()
				// assembly attributes
				?? impProperty.DeclaringType?.Assembly.GetCustomAttribute<T>()
				?? impType.Assembly.GetCustomAttribute<T>()
				?? svcType.Assembly.GetCustomAttribute<T>();
#endif
		}

		private static T FindAttribute<T>(Type svcType, Type impType, MethodInfo method) where T : Attribute
		{
			MethodInfo[] methods = GetInterfaces(svcType, impType, method).ToArray();
			MethodInfo[] impMethods = GetImplementations(svcType, impType, method).ToArray();
			return
				// method annotations
				impMethods.Select(o => o.GetCustomAttribute<T>(true)).FirstOrDefault()
				?? method.GetCustomAttribute<T>(true)
#if NETSTANDARD1_6
				// class / interface annotations
				?? impMethods.Select(o => o.DeclaringType?.GetTypeInfo().GetCustomAttribute<T>(true)).FirstOrDefault()
				?? methods.Select(o => o.DeclaringType?.GetTypeInfo().GetCustomAttribute<T>(true)).FirstOrDefault()
				?? impType.GetTypeInfo().GetCustomAttribute<T>(true)
				?? svcType.GetTypeInfo().GetCustomAttribute<T>(true)
				// module attributes
				?? impMethods.Select(o => o.DeclaringType?.GetTypeInfo().Module.GetCustomAttribute<T>()).FirstOrDefault()
				?? methods.Select(o => o.DeclaringType?.GetTypeInfo().Module.GetCustomAttribute<T>()).FirstOrDefault()
				?? impType.GetTypeInfo().Module.GetCustomAttribute<T>()
				?? svcType.GetTypeInfo().Module.GetCustomAttribute<T>()
				// assembly attributes
				?? impMethods.Select(o => o.DeclaringType?.GetTypeInfo().Assembly.GetCustomAttribute<T>()).FirstOrDefault()
				?? methods.Select(o => o.DeclaringType?.GetTypeInfo().Assembly.GetCustomAttribute<T>()).FirstOrDefault()
				?? impType.GetTypeInfo().Assembly.GetCustomAttribute<T>()
				?? svcType.GetTypeInfo().Assembly.GetCustomAttribute<T>();
#else
				// class / interface annotations
				?? impMethods.Select(o => o.DeclaringType?.GetCustomAttribute<T>(true)).FirstOrDefault()
				?? methods.Select(o => o.DeclaringType?.GetCustomAttribute<T>(true)).FirstOrDefault()
				?? impType.GetCustomAttribute<T>(true)
				?? svcType.GetCustomAttribute<T>(true)
				// module attributes
				?? impMethods.Select(o => o.DeclaringType?.Module.GetCustomAttribute<T>()).FirstOrDefault()
				?? methods.Select(o => o.DeclaringType?.Module.GetCustomAttribute<T>()).FirstOrDefault()
				?? impType.Module.GetCustomAttribute<T>()
				?? svcType.Module.GetCustomAttribute<T>()
				// assembly attributes
				?? impMethods.Select(o => o.DeclaringType?.Assembly.GetCustomAttribute<T>()).FirstOrDefault()
				?? methods.Select(o => o.DeclaringType?.Assembly.GetCustomAttribute<T>()).FirstOrDefault()
				?? impType.Assembly.GetCustomAttribute<T>()
				?? svcType.Assembly.GetCustomAttribute<T>();
#endif
		}

		private static FieldBuilder GetCacheField(GeneratorContext ctx, Type type)
			=> ctx.CacheFields.GetOrAdd(type, type =>
				{
					return ctx.TypeBuilder.DefineField("__" + type.GetFieldName(),
						type,
						FieldAttributes.Private |
						FieldAttributes.InitOnly);
				});

#region Static

		private static MethodInfo generateKeyMethod;
		private readonly static object generateKeyMethodLock = new object();
		private static MethodInfo GenerateKeyMethod
		{
			get
			{
				if (generateKeyMethod == null)
					lock (generateKeyMethodLock)
						if (generateKeyMethod == null)
						{
							generateKeyMethod = typeof(CachedServiceBase)
#if NETSTANDARD1_6
								.GetTypeInfo()
#endif
								.GetMethod(
									nameof(CachedServiceBase.GenerateKey),
									new[] { typeof(object[]) })
									?? throw new InvalidOperationException($"Unable to find {nameof(CachedServiceBase.GenerateKey)} method.");
						}
				return generateKeyMethod;
			}
		}

		private static ConstructorInfo ArgumentNullExceptionCtor
			=> typeof(ArgumentNullException)
#if NETSTANDARD1_6
				.GetTypeInfo()
#endif
				.GetConstructor(new[] { typeof(string) });

		private static MethodInfo NonCachedMethod(Type t)
			=> t
#if NETSTANDARD1_6
				.GetTypeInfo()
#endif
				.GetMethod("get_" + nameof(CachedServiceBase<object>.NonCached));

#endregion
	}
}
