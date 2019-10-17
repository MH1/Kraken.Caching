using Kraken.Caching.Engine;
using System;
using System.Collections.Concurrent;

namespace Kraken.Caching
{
	internal class CachedGenerator<TService, TImplementation> : CachedGenerator
		where TService : class
		where TImplementation : class, TService
	{
		public CachedGenerator() : base(typeof(TService), typeof(TImplementation))
		{ }
	}

	internal class CachedGenerator
	{
		public CachedGenerator(Type svcType, Type impType)
		{
			Tuple<Type, Type> key = Tuple.Create(svcType, impType);
			this.Type = Types.GetOrAdd(key,
				o => Generator.Instance.GenerateType(key.Item1, key.Item2));
		}

		public Type Type { get; }

		private static readonly ConcurrentDictionary<Tuple<Type, Type>, Type> Types = new ConcurrentDictionary<Tuple<Type, Type>, Type>();
	}
}
