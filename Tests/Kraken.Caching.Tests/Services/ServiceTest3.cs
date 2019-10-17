using System;

namespace Kraken.Caching.Tests.Services
{
	public class ServiceTest3 : ServiceTest, IServiceTest3
	{
		public ServiceTest3(IServiceProvider provider) : base(provider) { }
	}
}
