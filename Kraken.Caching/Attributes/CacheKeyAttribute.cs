using System;

namespace Kraken.Caching
{
	[AttributeUsage(
		AttributeTargets.Class |
		AttributeTargets.Interface |
		AttributeTargets.Method |
		AttributeTargets.Property,
		AllowMultiple = false,
		Inherited = true)]
	public sealed class CacheKeyAttribute : Attribute
	{
		public CacheKeyAttribute(string cacheKey)
		{
			this.CacheKey = cacheKey;
		}

		public string CacheKey { get; private set; }
	}
}
