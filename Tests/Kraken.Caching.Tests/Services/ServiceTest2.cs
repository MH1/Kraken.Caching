using System;

namespace Kraken.Caching.Tests.Services
{
	[CacheKey("ServiceTest2")]
	public class ServiceTest2 : ServiceTest, IServiceTest2
	{
		public ServiceTest2(IServiceProvider provider) : base(provider) { }
	}
}
