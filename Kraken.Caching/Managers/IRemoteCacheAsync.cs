using System.Threading.Tasks;

namespace Kraken.Caching
{
	public interface IRemoteCacheAsync
	{
		bool Contains(string key);

		Task<T> LoadAsync<T>(CacheParams par);

		Task SaveAsync<T>(CacheParams par, T value);

		Task InvalidateAsync(CacheParams par);
	}
}
