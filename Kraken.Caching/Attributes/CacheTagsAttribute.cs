using System;
using System.Collections.Generic;
using System.Linq;

namespace Kraken.Caching
{
	[AttributeUsage(
		AttributeTargets.Assembly |
		AttributeTargets.Module |
		AttributeTargets.Class |
		AttributeTargets.Interface |
		AttributeTargets.Method |
		AttributeTargets.Property,
		AllowMultiple = true,
		Inherited = true)]
	public sealed class CacheTagsAttribute : Attribute
	{
		public CacheTagsAttribute(params object[] tags)
		{
			this.Tags = tags.Where(o => o != null);
		}

		public IEnumerable<object> Tags { get; private set; }
	}
}
