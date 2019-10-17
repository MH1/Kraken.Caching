using System;

namespace Kraken.Caching
{
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public sealed class CacheIgnoreAttribute : Attribute
	{
	}
}
