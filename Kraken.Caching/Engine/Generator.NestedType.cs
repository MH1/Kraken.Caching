using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Kraken.Caching.Engine
{
	internal partial class Generator
	{
		private int index = 0;
		private readonly object indexLock = new object();

		private void GenerateNestedType(ref GeneratorContext ctx, MethodInfo method, MethodInfo methodImpl)
		{
			int idx;
			lock (indexLock)
			{
				idx = index++;
			}

			TypeBuilder builder = ctx.Module.DefineType($"<>c__CacheDelegate_{method.Name}_{idx}",
				TypeAttributes.Class |
				TypeAttributes.NotPublic |
				TypeAttributes.AnsiClass |
				TypeAttributes.AutoClass |
				TypeAttributes.Sealed |
				TypeAttributes.BeforeFieldInit);
			builder.SetCustomAttribute(new CustomAttributeBuilder(
					typeof(CompilerGeneratedAttribute)
#if NETSTANDARD1_6
					.GetTypeInfo()
#endif
					.GetConstructor(Type.EmptyTypes),
						new object[0]));

			// add generic parameters
			GenericTypeParameterBuilder[] genPar = null;
			if (method.IsGenericMethod)
			{
				genPar = builder.DefineGenericParameters(ctx.GenericParameterNames);
				for (int i = 0; i < ctx.GenericParameterNames.Length; i++)
				{
					genPar[i].SetGenericParameterAttributes(ctx.GenericArguments[i]
#if NETSTANDARD1_6
						.GetTypeInfo()
#endif
						.GenericParameterAttributes);
					genPar[i].SetInterfaceConstraints(ctx.GenericArguments[i]
#if NETSTANDARD1_6
						.GetTypeInfo()
#endif
						.GetGenericParameterConstraints());
				}
			}

			//FieldBuilder asyncState = null;
			//FieldBuilder asyncBuilder = null;
			//if (isAsync)
			//{
			//	asyncState = nested.DefineField("<>1__state", typeof(int), FieldAttributes.Public);
			//	asyncBuilder = nested.DefineField("<>t__builder",
			//		typeof(AsyncTaskMethodBuilder<>).MakeGenericType(method.ReturnType),
			//		FieldAttributes.Public);
			//}

			#region Generate nested fields and key array

			List<string> parameters = method.GetParameters()
				.Where(o => o.GetCustomAttribute<CacheIgnoreAttribute>() == null)
				.Select(o => o.Name).ToList();
			parameters = methodImpl.GetParameters()
				.Where(o => o.GetCustomAttribute<CacheIgnoreAttribute>() == null)
				.Where(o => parameters.Contains(o.Name))
				.Select(o => o.Name).ToList();

			foreach (ParameterInfo par in method.GetParameters())
			{
				FieldBuilder field = null;
#if !NETSTANDARD1_6
				if (genPar != null)
				{
					GenericTypeParameterBuilder b = genPar.SingleOrDefault(o => o.Name == par.ParameterType.Name);
					if (b != null)
						field = builder.DefineField(par.Name, b, FieldAttributes.Public);
				}
#else
#endif
				//if (field == null)
				field = builder.DefineField(par.Name, par.ParameterType, FieldAttributes.Public);

				ctx.NestedFields.Add(field);
				if (parameters.Contains(field.Name))
					ctx.NestedKeyFields.Add(field);
			}

#endregion

			ctx.NestedMethod = builder.DefineMethod($"<{method.Name}>b__0",
				MethodAttributes.Assembly |
				MethodAttributes.HideBySig,
				method.ReturnType, new[] { ctx.ServiceBase });

#region Delegate code

			ParameterBuilder parBuilder = ctx.NestedMethod.DefineParameter(1, ParameterAttributes.None, "svc");
			ILGenerator il = ctx.NestedMethod.GetILGenerator();
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Callvirt, NonCachedMethod(ctx.ServiceBase));
			foreach (FieldBuilder item in ctx.NestedFields)
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, item);
			}
			il.Emit(OpCodes.Callvirt, methodImpl);
			il.Emit(OpCodes.Ret);

			builder.DefineDefaultConstructor(MethodAttributes.Public);

			Type result = builder.CreateTypeInfo().AsType();

			if (result
#if NETSTANDARD1_6
					.GetTypeInfo()
#endif
					.IsGenericTypeDefinition)
				result.MakeGenericType(method.GetGenericArguments());

			ctx.NestedType = result;

#endregion
		}
	}
}
