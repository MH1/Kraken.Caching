using Kraken.Caching.Tests.Services;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kraken.Caching.Tests
{
	public class CachedService
	{
		private readonly IServiceTest service;
		private readonly ICacheManager memory, scoped;
		private readonly string KeyPrefix;

		public CachedService() : this(null, typeof(IServiceTest), typeof(ServiceTest)) { }

		protected CachedService(string keyPrefix, Type svcType, Type impType)
		{
			ServiceCollection builder = new ServiceCollection();
			builder.AddCaching();
			builder.AddCached(svcType, impType);
			IServiceProvider provider = builder.BuildServiceProvider(); // new ServiceProviderOptions { ValidateOnBuild = true });
			service = (IServiceTest)provider.GetService(svcType);
			memory = provider.GetService<MemoryCache>();
			scoped = provider.GetService<ScopedMemoryCache>();
			this.KeyPrefix = keyPrefix ?? impType.FullName;
		}

		[Test, Order(1)]
		public void ServiceCheck()
		{
			Assert.False(service is ServiceTest);
		}

#if NETCOREAPP1_1
		private static readonly DateTime date = new DateTime(2019, 12, 8);
#else
		private static readonly DateTime date = new DateTime(2019, 12, 8, CultureInfo.InvariantCulture.Calendar);
#endif

		#region Basic

		[Test, Order(2)]
		public void Basic()
		{
			string test1 = service.Method1(12345);
			Assert.AreEqual("12345", test1);
			TestCacheKeys("System.String Method1(Int32)|12345", memory, scoped);
		}

		[Test, Order(2)]
		public void Basic2()
		{
			int res = service.Method1("1234");
			Assert.AreEqual(123, res);
			TestCacheKeys("TestMethod_1|1234", memory, scoped);
		}

		[Test, Order(3)]
		public void Basic3()
		{
			int res = service.Method1(12, 23, "34", date);
			Assert.AreEqual(123123, res);
			TestCacheKeys("Int32 Method1(Int32, Int32, System.String, System.Object)|12|34|\"2019-12-08T00:00:00\"", scoped, memory);
		}

		#endregion

		#region Methods

		[Test, Order(2)]
		public void Method1()
		{
			service.Method1(12, 23, "34");
		}

		#endregion

		#region Properties

		[Test, Order(2)]
		public void Property1()
		{
			int res = service.Property1;
			Assert.AreEqual(133, res);
		}

		[Test, Order(2)]
		public void Property2()
		{
			string res = service.Property2;
			Assert.AreEqual("144", res);
		}

		[Test, Order(3)]
		public void Property1_Set()
		{
			service.Property1 = 11;
			int res = service.Property1;
			Assert.AreEqual(11, res);
		}

		[Test, Order(3)]
		public void Property2_Set()
		{
			service.Property2 = "test";
			string res = service.Property2;
			Assert.AreEqual("test", res);
		}

		#endregion

		#region Indexer

		[Test, Order(2)]
		public void Indexer()
		{
			string res = service[12, 23, "34", date, default(Guid)];
			Assert.AreEqual("this1223", res);
		}

		[Test, Order(3)]
		public void Indexer_Setter()
		{
			service[12, 23, "34", date, default(Guid)] = "test";
			//Assert.AreEqual("this1223", res);
			//TestCacheKeys("CF18DA71BD123C9BAD9840C9E1CB0912E00DD31C2EADC355349D3B13ABC52536|12|34|\"2019-12-08T00:00:00\"|\"00000000-0000-0000-0000-000000000000\"", scoped, memory);
		}

		//[Test, DependsOn(nameof(Indexer))]
		//public void Indexer_Cached()
		//{
		//	string res = service[12, 42, "34", DateTime.Now, Guid.NewGuid()];
		//	Assert.AreEqual("this1223", res);
		//	//TestCacheKeys("CF18DA71BD123C9BAD9840C9E1CB0912E00DD31C2EADC355349D3B13ABC52536|12|34|\"2019-12-08T00:00:00\"|\"00000000-0000-0000-0000-000000000000\"", scoped, memory);
		//}

		//[Test, DependsOn(nameof(Indexer_Cached))]
		//public void Indexer_Invalidate()
		//{
		//	// invalidate key
		//	service[12, 42, "34", DateTime.Now, Guid.NewGuid()] = "test";
		//	string res = service[12, 42, "34", DateTime.Now, Guid.NewGuid()];
		//	Assert.AreEqual("test341242", res);
		//	//TestCacheKeys("CF18DA71BD123C9BAD9840C9E1CB0912E00DD31C2EADC355349D3B13ABC52536|12|34|\"2019-12-08T00:00:00\"|\"00000000-0000-0000-0000-000000000000\"", scoped, memory);
		//}

		#endregion

		#region Generic parameters

		[Test, Order(2)]
		public void GenericMethod1()
		{
			DateTime res = service.Method1<string, DateTime>("t4");
			Assert.AreEqual(default(DateTime), res);
		}

		[Test, Order(2)]
		public void GenericMethod2()
		{
			Guid res = service.Method2<int, Guid>(552);
			Assert.AreEqual(default(Guid), res);
		}

		#endregion

		#region Methods with output parameters

		[Test, Order(2)]
		public void MethodWithRef()
		{
			string res2 = "123";
			int res1 = service.Method1(12, 23, "34", Guid.NewGuid(), ref res2);
			Assert.AreEqual(444, res1);
			Assert.AreEqual("4441", res2);
		}

		[Test, Order(2)]
		public void MethodWithOut()
		{
			int res1 = service.Method2(12, 23, "34", Guid.NewGuid(), out string res2);
			Assert.AreEqual(555, res1);
			Assert.AreEqual("5551", res2);
		}

		#endregion

		#region Async

		[Test, Order(2)]
		public async Task Async1()
		{
			await service.Method1(12, 23, 34);
		}

		[Test, Order(2)]
		public async Task Async2()
		{
			string res = await service.Method1(12, "23", 34);
			Assert.AreEqual("test2", res);
		}

		[Test, Order(2)]
		public async Task Async3()
		{
			DateTime res = await service.Method1<string, DateTime>("12", "23", 34);
			Assert.AreEqual(default(DateTime), res);
		}

#if NETCOREAPP3_0
		[Test, Order(2)]
		public async Task AsyncCollection()
		{
			await foreach (int item in service.MethodAsyncEnumerable())
			{
			}
		}
#endif

		#endregion

		#region Collections

		[Test, Order(2)]
		public void Enumerable()
		{
			IEnumerable<int> res = service.MethodEnumerable();
			List<int> list = res.ToList();
			Assert.Contains(3, list);
			Assert.Contains(6, list);
			Assert.Contains(9, list);
		}

		[Test, Order(3)]
		public void List1()
		{
			List<int> res = service.MethodList().ToList();
			Assert.Contains(3, res);
			Assert.Contains(6, res);
			Assert.Contains(9, res);
		}

		[Test, Order(4)]
		public void ListItem1()
		{
			int res = service.ListItem2();
			Assert.AreEqual(6, res);
		}

		#endregion

		[Test, Order(int.MaxValue)]
		public void Invalidation()
		{
			Thread.Sleep(60 * 1000 * 11 / 10);
			lock (testKeyLock)
				foreach (string key in testKeys)
				{
					Assert.False(memory.ContainsKey(key));
					Assert.False(scoped.ContainsKey(key));
				}
		}

		private readonly IList<string> testKeys = new List<string>();
		private readonly object testKeyLock = new object();
		private void TestCacheKeys(string key, ICacheManager manager, params ICacheManager[] others)
		{
			if (string.IsNullOrEmpty(key))
				return;
			key = $"{this.KeyPrefix}|{key}";
			Assert.True(manager.ContainsKey(key));
			foreach (ICacheManager mgr in others)
			{
				Assert.False(mgr.ContainsKey(key));
			}
			if (!testKeys.Contains(key))
				lock (testKeyLock)
					if (!testKeys.Contains(key))
						testKeys.Add(key);
		}
	}
}
