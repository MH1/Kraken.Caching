using System;
using System.Threading.Tasks;

namespace Kraken.Caching
{
	public abstract class RemoteCacheManagerBase : ICacheManager
	{
		private readonly ScopedMemoryCache memoryCache;

		public RemoteCacheManagerBase(ScopedMemoryCache memoryCache)
		{
			this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
		}

		#region Sync

		public TResult Get<TService, TResult>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			Func<CachedServiceBase<TService>, TResult> getData)
			where TService : class
			=> memoryCache.Get(cached, par, service =>
			{
				TResult data = default;
				if (this is IRemoteCache remote)
				{
					if (remote.Contains(par.Key))
					{
						data = remote.Load<TResult>(par);
						return data;
					}
				}
				else if (this is IRemoteCacheAsync remoteAsync)
				{
					if (remoteAsync.Contains(par.Key))
					{
						Task<TResult> task = remoteAsync.LoadAsync<TResult>(par);
						task.Wait();
						data = task.Result;
						return data;
					}
				}
				else throw new InvalidOperationException($"Manager {GetType().FullName} has is not implemented.");

				data = getData(cached);
				if (this is IRemoteCache remote0)
					remote0.Save(par, data);
				else if (this is IRemoteCacheAsync remoteAsync0)
				{
					Task task = remoteAsync0.SaveAsync(par, data);
					task.Wait();
				}
				return data;
			});

		public void Set<TService, TResult>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			TResult value,
			Action<CachedServiceBase<TService>, TResult> setData)
			where TService : class
			=> memoryCache.Set(cached, par, value, (service, value) =>
			{
				setData(service, value);
				if (this is IRemoteCache remote0)
					remote0.Save(par, value);
				else if (this is IRemoteCacheAsync remoteAsync0)
				{
					Task task = remoteAsync0.SaveAsync(par, value);
					task.Wait();
				}
			});

		public void Set<TService>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			Action<CachedServiceBase<TService>> setData)
			where TService : class
			=> memoryCache.Set(cached, par, service =>
			{
				setData(service);
				if (this is IRemoteCache remote0)
					remote0.Invalidate(par);
				else if (this is IRemoteCacheAsync remoteAsync0)
				{
					Task task = remoteAsync0.InvalidateAsync(par);
					task.Wait();
				}
			});

		#endregion

		#region Async

		public async Task<TResult> GetAsync<TService, TResult>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			Func<CachedServiceBase<TService>, Task<TResult>> getData)
			where TService : class
			=> await memoryCache.GetAsync(cached, par, async service =>
			{
				if (this is IRemoteCacheAsync remoteAsync)
				{
					if (remoteAsync.Contains(par.Key))
						return await remoteAsync.LoadAsync<TResult>(par);
				}
				else if (this is IRemoteCache remote)
				{
					if (remote.Contains(par.Key))
						return await Task.FromResult(remote.Load<TResult>(par));
				}
				else
					throw new InvalidOperationException($"Manager {GetType().FullName} has is not implemented.");

				TResult data = await getData(cached);
				if (this is IRemoteCacheAsync remoteAsync0)
					await remoteAsync0.SaveAsync(par, data);
				else if (this is IRemoteCache remote0)
					remote0.Save(par, data);
				return await Task.FromResult(data);
			});

		public async Task SetAsync<TService, TResult>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			TResult value,
			Func<CachedServiceBase<TService>, TResult, Task> setData)
			where TService : class
			=> await memoryCache.SetAsync(cached, par, value, async (service, value) =>
			{
				await setData(service, value);
				if (this is IRemoteCacheAsync remoteAsync0)
					await remoteAsync0.SaveAsync(par, value);
				else if (this is IRemoteCache remote0)
					remote0.Save(par, value);
			});

		public async Task SetAsync<TService>(
			CachedServiceBase<TService> cached,
			CacheParams par,
			Func<CachedServiceBase<TService>, Task> setData)
			where TService : class
			=> await memoryCache.SetAsync(cached, par, async service =>
			{
				await setData(service);
				if (this is IRemoteCacheAsync remoteAsync0)
					await remoteAsync0.InvalidateAsync(par);
				else if (this is IRemoteCache remote0)
					remote0.Invalidate(par);
			});

		#endregion

		public bool ContainsKey(string key)
		{
			if (this is IRemoteCache remote)
				return remote.Contains(key);
			else if (this is IRemoteCacheAsync remoteAsync)
				return remoteAsync.Contains(key);
			else
				throw new InvalidOperationException($"Manager {GetType().FullName} has is not implemented.");
		}
	}
}
