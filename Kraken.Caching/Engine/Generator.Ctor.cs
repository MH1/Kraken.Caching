using Kraken.Caching.Reflection;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Kraken.Caching.Engine
{
	internal partial class Generator
	{
		private void GenerateConstructor(GeneratorContext ctx)
		{
			Type woCache = typeof(NonCached<>).MakeGenericType(ctx.ImplementationType);
			Type[] args = new[] { woCache }.Union(ctx.CacheFields.Keys).ToArray();
			
			ConstructorBuilder ctor = ctx.TypeBuilder.DefineConstructor(
				MethodAttributes.Public |
				MethodAttributes.HideBySig |
				MethodAttributes.SpecialName |
				MethodAttributes.RTSpecialName,
				CallingConventions.Standard,
				args);
			ILGenerator il = ctor.GetILGenerator();

			// base()
			il.Emit(OpCodes.Ldarg_0);

			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Call, ctx.ServiceBase
#if NETSTANDARD1_6
				.GetTypeInfo()
#endif
				.GetConstructor(new[] { woCache }));
			il.Emit(OpCodes.Nop);
			il.Emit(OpCodes.Nop);

			int num = 2;
			foreach (Type key in ctx.CacheFields.Keys)
			{
				// this.cacheHandler = cacheHandler ?? throw new ArgumentNullException(nameof(cacheHandler));
				FieldBuilder field = ctx.CacheFields[key];
				Label lab = il.DefineLabel();
				il.Emit(OpCodes.Ldarg_0);
				il.Emit_Ldarg(num);
				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Brtrue_S, lab);
				il.Emit(OpCodes.Pop);
				il.Emit(OpCodes.Ldstr, key.GetFieldName());
				il.Emit(OpCodes.Newobj, ArgumentNullExceptionCtor);
				il.Emit(OpCodes.Throw);
				il.MarkLabel(lab);
				il.Emit(OpCodes.Stfld, field);
				num++;
			}

			il.Emit(OpCodes.Ret);
		}
	}
}
