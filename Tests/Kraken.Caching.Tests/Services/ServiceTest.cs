using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kraken.Caching.Tests.Services
{
	public class ServiceTest : IServiceTest
	{
		private readonly IServiceProvider provider;

		public ServiceTest(IServiceProvider provider)
		{
			this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
		}

		public string Test() => "1234";

		public void Test2() { }

		public string Method1(int par) => "12345";

		public void Method1(int par1, [CacheIgnore] int par2, string par3) { }

		public int Method1(string par) => 123;

		public int Method1(int par1, int par2, string par3, object par4) => 123123;

		public int Method1(int par1, [CacheIgnore] int par2, string par3, object par4, ref string result)
		{
			result = "4441";
			return 444;
		}

		public int Method2(int par1, [CacheIgnore] int par2, string par3, object par4, out string result)
		{
			result = "5551";
			return 555;
		}

		public T2 Method1<T1, T2>(T1 par) => default;

		public T2 Method2<T1, T2>(T1 par) => default;

		public Task Method1(int par1, [CacheIgnore] int par2, int par3)
#if NET45 || NET452
			=> Task.Delay(0);
#else
			=> Task.CompletedTask;
#endif

		public Task<string> Method1(int par1, [CacheIgnore] string par2, int par3) => Task.FromResult("test2");

		public Task<T2> Method1<T1, T2>(T1 par1, [CacheIgnore] string par2, int par3) => Task.FromResult(default(T2));

		public IEnumerable<int> MethodEnumerable()
		{
			yield return 3;
			yield return 6;
			yield return 9;
		}

#if NETCOREAPP3_0
		public async IAsyncEnumerable<int> MethodAsyncEnumerable()
		{
			yield return 3;
			yield return 6;
			yield return 9;
			await Task.CompletedTask;
		}
#endif

		public IList<int> MethodList()
		{
			IEnumerable<int> result = MethodEnumerable();
			return result.ToList();
		}

		public int ListItem2()
		{
			return MethodList()?.Skip(1).First()
				?? throw new InvalidOperationException("Cache returned null.");
		}

		private string prefix = "this";

		public string this[int par1, [CacheIgnore] int par2, string par3, DateTime par4, object par5]
		{
			get => $"{prefix}{par1}{par2}";
			set => this.prefix = $"{value}{par3}";
		}

		public int Property1 { get; set; } = 133;

		public string Property2 { get; set; } = "144";
	}
}
