using System;
using System.Reflection;

namespace Kraken.Caching
{
	[AttributeUsage(
		AttributeTargets.Assembly |
		AttributeTargets.Module |
		AttributeTargets.Class |
		AttributeTargets.Interface |
		AttributeTargets.Method |
		AttributeTargets.Property,
		AllowMultiple = false,
		Inherited = true)]
	public class CachedAttribute : Attribute
	{
		public CachedAttribute(Type manager, int durability)
		{
			if (manager == null)
				throw new ArgumentNullException(nameof(manager));
			if (!typeof(ICacheManager)
#if NETSTANDARD1_6
					.GetTypeInfo()
#endif
					.IsAssignableFrom(manager))
				throw new ArgumentException($"Type '{manager.FullName}' does not implement {nameof(ICacheManager)}.");

				this.Manager = manager;
			this.Durability = durability;
		}

		public Type Manager { get; }
		public int Durability { get; }
	}
}
