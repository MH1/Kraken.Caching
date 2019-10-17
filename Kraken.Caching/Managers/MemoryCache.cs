using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kraken.Caching
{
	public class MemoryCache : ICacheManager
	{
		private readonly IDictionary<string, CachedObject> localCache = new Dictionary<string, CachedObject>();
		private readonly object cacheLock = new object();

		#region Sync

		public TResult Get<TService, TResult>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			Func<CachedServiceBase<TService>, TResult> getData)
			where TService : class
		{
			if (localCache.ContainsKey(par.Key))
				lock (cacheLock)
					if (localCache.ContainsKey(par.Key))
					{
						CachedObject item = localCache[par.Key] as CachedObject;
						if (!item.IsExpired)
							return (TResult)item.Data;
					}
			TResult data = getData(cached);
			Save(par, data);
			return data;
		}

		public void Set<TService, TResult>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			TResult value,
			Action<CachedServiceBase<TService>, TResult> setData)
			where TService : class
		{
			setData(cached, value);
			Save(par, value);
		}

		public void Set<TService>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			Action<CachedServiceBase<TService>> setData)
			where TService : class
		{
			setData(cached);
			Invalidate(par);
		}

		#endregion

		#region Async

		public async Task<TResult> GetAsync<TService, TResult>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			Func<CachedServiceBase<TService>, Task<TResult>> getData)
			where TService : class
		{
			if (localCache.ContainsKey(par.Key))
				lock (cacheLock)
					if (localCache.ContainsKey(par.Key))
					{
						CachedObject item = localCache[par.Key] as CachedObject;
						if (!item.IsExpired)
							return (TResult)item.Data;
					}
			TResult data = await getData(cached);
			Save(par, data);
			return data;
		}

		public async Task SetAsync<TService, TResult>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			TResult value,
			Func<CachedServiceBase<TService>, TResult, Task> setData)
			where TService : class
		{
			await setData(cached, value);
			Save(par, value);
		}

		public async Task SetAsync<TService>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			Func<CachedServiceBase<TService>, Task> setData)
			where TService : class
		{
			await setData(cached);
			Invalidate(par);
		}

		#endregion

		public bool ContainsKey(string key)
		{
			if (localCache.ContainsKey(key))
				lock (cacheLock)
					if (localCache.ContainsKey(key))
					{
						CachedObject item = localCache[key] as CachedObject;
						return !item.IsExpired;
					}
			return false;
		}

		#region Private methods

		private void Save<TResult>(CacheParams par, TResult data)
		{
			bool exists = true;
			if (!localCache.ContainsKey(par.Key))
				lock (cacheLock)
					if (!localCache.ContainsKey(par.Key))
					{
						exists = false;
						CachedObject<TResult> item = new CachedObject<TResult>
						{
							Data = data,
							Tags = par.Tags,
							ExpirationDate = DateTime.UtcNow.AddMinutes(par.Duration),
						};
						localCache.Add(par.Key, item);
					}

			if (exists)
			{
				CachedObject<TResult> item = localCache[par.Key] as CachedObject<TResult>;
				lock (item.Lock)
				{
					if (item.Data is IDisposable disposable)
						disposable.Dispose();
					item.Data = data;
					item.Tags = par.Tags;
					item.ExpirationDate = DateTime.UtcNow.AddMinutes(par.Duration);
				}
			}
		}

		private void Invalidate(CacheParams par)
		{
			if (localCache.ContainsKey(par.Key))
				lock (cacheLock)
					if (localCache.ContainsKey(par.Key))
					{
						CachedObject item = localCache[par.Key] as CachedObject;
						if (item is IDisposable disposable)
							disposable.Dispose();
						localCache.Remove(par.Key);
					}
		}

		#endregion
	}
}
