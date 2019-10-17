using System;

namespace Kraken.Caching
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property,
		AllowMultiple = false,
		Inherited = true)]
	public sealed class NonCachedAttribute : Attribute
	{
	}
}
