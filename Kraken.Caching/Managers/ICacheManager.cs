using System;
using System.Threading.Tasks;

namespace Kraken.Caching
{
	public interface ICacheManager
	{
		TResult Get<TService, TResult>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			Func<CachedServiceBase<TService>, TResult> getData)
			where TService : class;

		void Set<TService, TResult>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			TResult value,
			Action<CachedServiceBase<TService>, TResult> setData)
			where TService : class;

		void Set<TService>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			Action<CachedServiceBase<TService>> setData)
			where TService : class;

		Task<TResult> GetAsync<TService, TResult>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			Func<CachedServiceBase<TService>, Task<TResult>> getData)
			where TService : class;

		Task SetAsync<TService, TResult>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			TResult value,
			Func<CachedServiceBase<TService>, TResult, Task> setData)
			where TService : class;

		Task SetAsync<TService>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			Func<CachedServiceBase<TService>, Task> setData)
			where TService : class;

		bool ContainsKey(string key);
	}
}
