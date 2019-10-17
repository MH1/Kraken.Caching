using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kraken.Caching.Tests.Services
{
	[Cached(typeof(MemoryCache), 1)]
	public interface IServiceTest
	{
		string Test();
		void Test2();
		string Method1(int par);

		void Method1(int par1, [CacheIgnore]int par2, string par3);

		[CacheKey("TestMethod_1")]
		int Method1(string par);

		#region Collections

		IEnumerable<int> MethodEnumerable();

#if NETCOREAPP3_0
		IAsyncEnumerable<int> MethodAsyncEnumerable();
#endif

		IList<int> MethodList();

		int ListItem2();

		#endregion

		[Cached(typeof(ScopedMemoryCache), 1)]
		int Method1(int par1, [CacheIgnore]int par2, string par3, object par4);

		[Cached(typeof(ScopedMemoryCache), 1)]
		int Method1(int par1, [CacheIgnore]int par2, string par3, object par4, ref string result);

		[Cached(typeof(ScopedMemoryCache), 1)]
		int Method2(int par1, [CacheIgnore]int par2, string par3, object par4, out string result);

		Task Method1(int par1, [CacheIgnore]int par2, int par3);

		Task<string> Method1(int par1, [CacheIgnore]string par2, int par3);

		Task<T2> Method1<T1, T2>(T1 par1, [CacheIgnore] string par2, int par3);

		T2 Method1<T1, T2>(T1 par);

		[Cached(typeof(ScopedMemoryCache), 1)]
		T2 Method2<T1, T2>(T1 par);

		int Property1 { get; set; }

		[Cached(typeof(ScopedMemoryCache), 1)]
		string Property2 { get; set; }

		[Cached(typeof(ScopedMemoryCache), 1)]
		string this[int par1, [CacheIgnore]int par2, string par3, DateTime par4, object par5] { get; set; }
	}
}
