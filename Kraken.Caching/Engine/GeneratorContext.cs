using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Kraken.Caching.Engine
{
	internal class GeneratorContext
	{
		internal Type ServiceType;

		internal Type ImplementationType;

		internal ModuleBuilder Module;

		internal TypeBuilder TypeBuilder;

		internal Type ServiceBase;

		internal Type NestedType;

		internal MethodBuilder NestedMethod;

		internal string[] GenericParameterNames;

		internal Type[] GenericArguments;
		
		internal Type Handler;
		
		internal readonly ConcurrentDictionary<Type, FieldBuilder> CacheFields = new ConcurrentDictionary<Type, FieldBuilder>();
		
		internal readonly IList<FieldBuilder> NestedFields = new List<FieldBuilder>();
		
		internal readonly IList<FieldBuilder> NestedKeyFields = new List<FieldBuilder>();
	}
}