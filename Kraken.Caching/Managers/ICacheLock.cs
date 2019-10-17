namespace Kraken.Caching
{
	internal interface ICacheLock
	{
		void Lock(string key);
		void Unlock(string key);
	}
}