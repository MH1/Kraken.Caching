using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Kraken.Caching.Engine
{
	internal partial class Generator
	{
		private const int DefaultDurability = 5;

		private MethodBuilder GenerateMethod(GeneratorContext ctx, MethodInfo method, PropertyInfo property = null)
		{
			MethodInfo methodImpl = GetImplementation(ctx.ServiceType, ctx.ImplementationType, method);
			PropertyInfo propertyImpl = property != null ? GetImplementation(ctx.ServiceType, ctx.ImplementationType, property) : null;

			MethodBuilder methodBuilder = ctx.TypeBuilder.DefineMethod(method.Name,
				method.Attributes & ~MethodAttributes.Abstract,
				method.ReturnType,
				method.GetParameters().Select(o => o.ParameterType).ToArray());

			ctx.GenericArguments = method.GetGenericArguments();
			ctx.GenericParameterNames = new string[0];
			if (method.IsGenericMethod)
			{
				ctx.GenericParameterNames = ctx.GenericArguments.Select(o => o.Name).ToArray();
				GenericTypeParameterBuilder[] genPar0 = methodBuilder.DefineGenericParameters(ctx.GenericParameterNames);
				for (int i = 0; i < ctx.GenericParameterNames.Length; i++)
				{
					genPar0[i].SetGenericParameterAttributes(ctx.GenericArguments[i]
#if NETSTANDARD1_6
						.GetTypeInfo()
#endif
						.GenericParameterAttributes);
					genPar0[i].SetInterfaceConstraints(ctx.GenericArguments[i]
#if NETSTANDARD1_6
						.GetTypeInfo()
#endif
						.GetGenericParameterConstraints());
				}
			}

			// cache handler discovery
			CachedAttribute cacheParam = property != null
				? FindAttribute<CachedAttribute>(ctx.ServiceType, ctx.ImplementationType, property)
				: FindAttribute<CachedAttribute>(ctx.ServiceType, ctx.ImplementationType, method);

			ctx.Handler = cacheParam?.Manager;

			bool isGetter = true;
			//bool isAsync = false;
			if (property != null)
			{
				ctx.Handler = null;
			}
			// methods with no result
			if (method.ReturnType == typeof(void))
			{
				//isGetter = false;
				//if (ctx.Property == null)
				ctx.Handler = null;
			}
			// cannot cache the enumerables
			if (method.ReturnType
#if NETSTANDARD1_6
					.GetTypeInfo()
#endif
					.IsGenericType &&
				(method.ReturnType.GetGenericTypeDefinition()
#if NETSTANDARD1_6
					.GetTypeInfo()
#endif
					.IsEquivalentTo(typeof(IEnumerable<>))
#if NETSTANDARD2_1
				|| method.ReturnType.GetGenericTypeDefinition()
					.IsEquivalentTo(typeof(IAsyncEnumerable<>))
#endif
				))
			{
				ctx.Handler = null;
			}
			// TODO generic methods are not supported yet
			if (method.IsGenericMethod)
			{
				ctx.Handler = null;
			}
			// TODO async methods are not supported yet
			if (method.ReturnType
#if NETSTANDARD1_6
					.GetTypeInfo()
#endif
					.IsEquivalentTo(typeof(Task)) ||
				(method.ReturnType
#if NETSTANDARD1_6
					.GetTypeInfo()
#endif
					.BaseType
#if NETSTANDARD1_6
					?.GetTypeInfo()
#endif
					?.IsEquivalentTo(typeof(Task)) ?? false))
			{
				//isAsync = true;
				ctx.Handler = null;
			}
			// reference and output parameters are not supported (yet?)
			if (method.GetParameters().Any(o => o.ParameterType.IsByRef))
			{
				//isGetter = true;
				ctx.Handler = null;
			}

			// direct proxy test
			//ctx.Handler = null;

			#region Without cache - direct call of non-cached variant

			ILGenerator il = methodBuilder.GetILGenerator();

			if (ctx.Handler == null)
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Call, NonCachedMethod(ctx.ServiceBase));
				int parCount = method.GetParameters().Length;
				if (property != null && !isGetter)
					parCount++;
				for (int i = 1; i <= parCount; i++)
				{
					il.Emit_Ldarg(i);
				}
				il.Emit(OpCodes.Callvirt, methodImpl);
				if (property != null && !isGetter)
				{
					il.Emit(OpCodes.Nop);
				}
				il.Emit(OpCodes.Ret);

				return methodBuilder;
			}

			if (property != null)
			{
				return isGetter
					? GeneratePropertyGetMethod(ctx, method, property)
					: GeneratePropertySetMethod(ctx, method, property);
			}

			#endregion
			ctx.NestedFields.Clear();
			ctx.NestedKeyFields.Clear();
			GenerateNestedType(ref ctx, method, methodImpl);

			FieldBuilder handler = GetCacheField(ctx, ctx.Handler);
			LocalBuilder local = il.DeclareLocal(ctx.NestedType);
			LocalBuilder param = il.DeclareLocal(typeof(CacheParams));
			FieldInfo keyField = typeof(CacheParams)
#if NETSTANDARD1_6
				.GetTypeInfo()
#endif
				.GetField(nameof(CacheParams.Key));
			FieldInfo tagsField = typeof(CacheParams)
#if NETSTANDARD1_6
				.GetTypeInfo()
#endif
				.GetField(nameof(CacheParams.Tags));
			FieldInfo durField = typeof(CacheParams)
#if NETSTANDARD1_6
				.GetTypeInfo()
#endif
				.GetField(nameof(CacheParams.Duration));

			il.Emit(OpCodes.Newobj, ctx.NestedType
#if NETSTANDARD1_6
				.GetTypeInfo()
#endif
				.GetConstructor(Type.EmptyTypes));
			il.Emit(OpCodes.Stloc_0);
			{
				int idx0 = 1;
				foreach (FieldBuilder field in ctx.NestedFields)
				{
					il.Emit(OpCodes.Ldloc_0);
					il.Emit_Ldarg(idx0);
					il.Emit(OpCodes.Stfld, field);
					idx0++;
				}
			}

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, handler);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Newobj, typeof(CacheParams)
#if NETSTANDARD1_6
				.GetTypeInfo()
#endif
				.GetConstructor(Type.EmptyTypes));
			il.Emit(OpCodes.Stloc_1);
			il.Emit(OpCodes.Ldloc_1);

			#region Generate cache key
			{
				int addBytes = 2;
				if (method.IsGenericMethod)
					addBytes += ctx.GenericArguments.Length;

				il.Emit_Ldc_I4(ctx.NestedKeyFields.Count + addBytes);
				il.Emit(OpCodes.Newarr, typeof(object));

				string svcKey = ctx.ServiceType
#if NETSTANDARD1_6
						.GetTypeInfo()
#endif
						.GetCustomAttribute<CacheKeyAttribute>()?.CacheKey
					?? ctx.ImplementationType
#if NETSTANDARD1_6
						.GetTypeInfo()
#endif
						.GetCustomAttribute<CacheKeyAttribute>()?.CacheKey
					?? ctx.ImplementationType.ToString();

				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Ldstr, svcKey);
				il.Emit(OpCodes.Stelem_Ref);

				// add generic type arguments from method
				int idx0 = 1;
				if (method.IsGenericMethod)
					foreach (Type gen in ctx.GenericArguments)
					{
						il.Emit(OpCodes.Dup);
						il.Emit_Ldc_I4(idx0);
						il.Emit(OpCodes.Ldtoken, gen);
						il.Emit(OpCodes.Call, typeof(Type)
#if NETSTANDARD1_6
							.GetTypeInfo()
#endif
							.GetMethod(nameof(Type.GetTypeFromHandle), new[] { typeof(RuntimeTypeHandle) }));
						il.Emit(OpCodes.Stelem_Ref);
						idx0++;
					}

				string methodKey =
					(methodImpl.GetCustomAttribute<CacheKeyAttribute>()
					?? method.GetCustomAttribute<CacheKeyAttribute>())?.CacheKey
					?? method.ToString();

				il.Emit(OpCodes.Dup);
				il.Emit_Ldc_I4(idx0);
				il.Emit(OpCodes.Ldstr, methodKey);
				il.Emit(OpCodes.Stelem_Ref);
				idx0++;

				foreach (FieldBuilder field in ctx.NestedKeyFields)
				{
					il.Emit(OpCodes.Dup);
					il.Emit_Ldc_I4(idx0);
					il.Emit(OpCodes.Ldloc_0);
					il.Emit(OpCodes.Ldfld, field);
					if (field.FieldType
#if NETSTANDARD1_6
							.GetTypeInfo()
#endif
							.IsValueType)
						il.Emit(OpCodes.Box, field.FieldType);
					il.Emit(OpCodes.Stelem_Ref);
					idx0++;
				}
				il.Emit(OpCodes.Call, GenerateKeyMethod);
				il.Emit(OpCodes.Stfld, keyField);
				il.Emit(OpCodes.Ldloc_1);
			}
			#endregion

			#region Generate tags array
			{
				object[] tags =
					(property != null
						? propertyImpl.GetCustomAttributes<CacheTagsAttribute>(true).Union(
							property.GetCustomAttributes<CacheTagsAttribute>(true))
						: methodImpl.GetCustomAttributes<CacheTagsAttribute>(true).Union(
							method.GetCustomAttributes<CacheTagsAttribute>(true))
					).Union(
						ctx.ImplementationType
#if NETSTANDARD1_6
							.GetTypeInfo()
#endif
							.GetCustomAttributes<CacheTagsAttribute>(true)).Union(
						ctx.ServiceType
#if NETSTANDARD1_6
							.GetTypeInfo()
#endif
							.GetCustomAttributes<CacheTagsAttribute>(true)).Union(
						ctx.ImplementationType
#if NETSTANDARD1_6
							.GetTypeInfo()
#endif
							.Module.GetCustomAttributes<CacheTagsAttribute>()).Union(
						ctx.ServiceType
#if NETSTANDARD1_6
							.GetTypeInfo()
#endif
							.Module.GetCustomAttributes<CacheTagsAttribute>()).Union(
						ctx.ImplementationType
#if NETSTANDARD1_6
							.GetTypeInfo()
#endif
							.Assembly.GetCustomAttributes<CacheTagsAttribute>()).Union(
						ctx.ServiceType
#if NETSTANDARD1_6
							.GetTypeInfo()
#endif
							.Assembly.GetCustomAttributes<CacheTagsAttribute>())
						.SelectMany(o => o.Tags)
						.Distinct()
						.ToArray();

				// pass tags to delegate
				il.Emit_Ldc_I4(tags.Length);
				il.Emit(OpCodes.Newarr, typeof(string));
				int idx0 = 0;
				foreach (object tag in tags)
				{
					il.Emit(OpCodes.Dup);
					il.Emit_Ldc_I4(idx0);
					il.Emit(OpCodes.Ldstr, CachedServiceBase.GetComponentKey(tag, -1));
					il.Emit(OpCodes.Stelem_Ref);
					idx0++;
				}
				il.Emit(OpCodes.Stfld, tagsField);
				il.Emit(OpCodes.Ldloc_1);
			}
			#endregion

			il.Emit_Ldc_I4(cacheParam?.Durability ?? DefaultDurability);
			il.Emit(OpCodes.Stfld, durField);
			il.Emit(OpCodes.Ldloc_1);

			#region Call delegate
			{
				il.Emit(OpCodes.Ldloc_0);
				il.Emit(OpCodes.Ldftn, ctx.NestedMethod);
				ConstructorInfo lambdaCtor;
				//if (isGetter)
				//{
				lambdaCtor = typeof(Func<,>)
					.MakeGenericType(ctx.ServiceBase, method.ReturnType)
#if NETSTANDARD1_6
					.GetTypeInfo()
#endif
					.GetConstructor(new[] { typeof(object), typeof(IntPtr) });
				//}
				//else
				//{
				//	Type[] genParamTypes = new[] { ctx.ServiceBase }.Union(ctx.NestedFields.Select(o => o.FieldType)).ToArray();
				//	Type lambda = typeof(Action).Assembly.GetTypes()
				//		.Where(o => o.Name == nameof(Action) &&
				//			o.GetGenericArguments().Length == genParamTypes.Length)
				//		.Single();
				//	lambda = lambda.MakeGenericType(genParamTypes);
				//	lambdaCtor = lambda
				//		.GetConstructor(new[] { typeof(object), typeof(IntPtr) });
				//}
				il.Emit(OpCodes.Newobj, lambdaCtor);

				string methodName =
					//isAsync
					//? isGetter ? nameof(ICacheManager.GetAsync) : nameof(ICacheManager.SetAsync)
					//: isGetter ? nameof(ICacheManager.Get) : nameof(ICacheManager.Set);
					isGetter ? nameof(ICacheManager.Get) : nameof(ICacheManager.Set);

				int parNum = isGetter ? 3 : 4;

				MethodInfo getMethod = typeof(ICacheManager)
#if NETSTANDARD1_6
						.GetTypeInfo()
#endif
						.GetMethods()
						.Where(o => o.IsGenericMethod &&
							o.GetGenericArguments().Length == 2 &&
							o.GetParameters().Length == parNum &&
							o.GetParameters()[1].ParameterType == typeof(CacheParams))
						.Single(o => o.Name == methodName);

				il.Emit(OpCodes.Callvirt, getMethod.MakeGenericMethod(new[] { ctx.ServiceBase, method.ReturnType }));
			}
			#endregion

			il.Emit(OpCodes.Ret);
			return methodBuilder;
		}

		public MethodBuilder GeneratePropertyGetMethod(GeneratorContext ctx, MethodInfo method, PropertyInfo property = null)
		{
			// TODO generate property getter method
			return null;
		}

		public MethodBuilder GeneratePropertySetMethod(GeneratorContext ctx, MethodInfo method, PropertyInfo property = null)
		{
			// TODO generate property setter method
			return null;
		}
	}
}
