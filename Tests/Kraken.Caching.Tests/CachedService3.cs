using Kraken.Caching.Tests.Services;

namespace Kraken.Caching.Tests
{
	public class CachedService3 : CachedService
	{
		public CachedService3() : base("ServiceTest3", typeof(IServiceTest3), typeof(ServiceTest3)) { }
	}
}
