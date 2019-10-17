using System;

namespace Kraken.Caching
{
	public class NonCached<TService>
		where TService : class
	{
		public NonCached(TService instance)
		{
			this.Instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		public TService Instance { get; private set; }
	}
}
