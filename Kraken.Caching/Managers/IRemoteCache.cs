namespace Kraken.Caching
{
	public interface IRemoteCache
	{
		bool Contains(string key);

		T Load<T>(CacheParams par);

		void Save<T>(CacheParams par, T value);

		void Invalidate(CacheParams par);
	}
}