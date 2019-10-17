using System;

namespace Kraken.Caching
{
	[Serializable]
	public class CachedObject
	{
		public object Data { get; set; }
		public string[] Tags { get; set; }
		public DateTime? ExpirationDate { get; set; }
		public bool IsExpired => ExpirationDate != null && DateTime.UtcNow > ExpirationDate;
		internal readonly object Lock = new object();
	}

	[Serializable]
	public class CachedObject<T> : CachedObject
	{
		public new T Data { get; set; }
	}
}
