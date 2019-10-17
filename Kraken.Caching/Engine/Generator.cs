using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Kraken.Caching.Engine
{
	internal partial class Generator
	{
		#region Singleton

		private Generator() { }

		public static Generator instance;
		private static readonly object instanceLock = new object();
		public static Generator Instance
		{
			get
			{
				if (instance == null)
					lock (instanceLock)
						if (instance == null)
							instance = new Generator();
				return instance;
			}
		}

		#endregion

		private readonly string AssemblyName = "Kraken.Caching.Runtime"; // + Guid.NewGuid().ToString().Replace("-", "");

		private ModuleBuilder module;
		private readonly object moduleLock = new object();

		internal Type GenerateType(Type svcType, Type impType)
		{
			if (!svcType
#if NETSTANDARD1_6
					.GetTypeInfo()
#endif
					.IsInterface)
				throw new ArgumentException($"Argument {svcType.FullName} is not interface.");
			if (impType
#if NETSTANDARD1_6
					.GetTypeInfo()
#endif
					.IsAbstract)
				throw new ArgumentNullException($"Unable to cache {impType.FullName} because it's abstract.");
			lock (moduleLock)
			{
				if (module == null)
				{
					// generate assembly and module
					AssemblyName assemblyName = new AssemblyName(AssemblyName);
					AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
					ModuleBuilder builder = asmBuilder.DefineDynamicModule("MainModule");
					module = builder;
				}

				GeneratorContext ctx = new GeneratorContext
				{
					Module = module,
					ServiceType = svcType,
					ImplementationType = impType,
					ServiceBase = typeof(CachedServiceBase<>).MakeGenericType(impType),
				};

				return GenerateType(ctx);
			}
		}

		private Type GenerateType(GeneratorContext ctx)
		{
			string typeName = GetUniqueName(ctx.Module, AssemblyName + "." + ctx.ImplementationType.FullName + "CacheWrapper");
			// generate class type
			ctx.TypeBuilder = ctx.Module.DefineType(typeName,
				TypeAttributes.Public |
				TypeAttributes.Class |
				TypeAttributes.AutoClass |
				TypeAttributes.AnsiClass |
				TypeAttributes.BeforeFieldInit |
				TypeAttributes.AutoLayout,
				ctx.ServiceBase,
				new Type[] { ctx.ServiceType });
			MethodInfo[] methods = ctx.ServiceType
#if NETSTANDARD1_6
				.GetTypeInfo()
#endif
				.GetMethods()
				.Union(
					ctx.ServiceType
#if NETSTANDARD1_6
						.GetTypeInfo()
#endif
						.GetInterfaces()
						.SelectMany(o => o
#if NETSTANDARD1_6
							.GetTypeInfo()
#endif
							.GetMethods()))
				.Distinct().ToArray();

			// generate method implementations
			foreach (MethodInfo method in methods)
				if ((method.Attributes & MethodAttributes.SpecialName) == 0)
					GenerateMethod(ctx, method);

			PropertyInfo[] properties = ctx.ServiceType
#if NETSTANDARD1_6
					.GetTypeInfo()
#endif
					.GetProperties()
	.Union(
		ctx.ServiceType
#if NETSTANDARD1_6
						.GetTypeInfo()
#endif
						.GetInterfaces()
			.SelectMany(o => o
#if NETSTANDARD1_6
							.GetTypeInfo()
#endif
							.GetProperties()))
	.Distinct().ToArray();

			// generate property implementations
			foreach (PropertyInfo property in properties)
				GenerateProperty(ctx, property);

			// generate constructor with DI parameters
			GenerateConstructor(ctx);

			// generate result type
			return ctx.TypeBuilder.CreateTypeInfo().AsType();
		}

		private string GetUniqueName(ModuleBuilder parent, string suggested)
		{
			if (parent.GetType(suggested) == null)
				return suggested;
			int num = 2;
			while (parent.GetType(suggested + "_" + num) != null)
				num++;
			return suggested + "_" + num;
		}
	}
}
