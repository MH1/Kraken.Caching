using Kraken.Caching.Tests.Services;

namespace Kraken.Caching.Tests
{
	public class CachedService2 : CachedService
	{
		public CachedService2() : base("ServiceTest2", typeof(IServiceTest2), typeof(ServiceTest2)) { }
	}
}
