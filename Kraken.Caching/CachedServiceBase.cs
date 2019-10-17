using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Kraken.Caching
{
	public abstract class CachedServiceBase
	{
		public static string GenerateKey(params object[] par)
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach (object item in par)
			{
				if (!first) sb.Append("|");
				sb.Append(GetComponentKey(item));
				first = false;
			}
			return sb.ToString();
		}

		internal static string GetComponentKey(object item, int maxLength = 64 * 11 / 10)
		{
			string result;
			if (item == null)
				return "null";
			if (item is string)
				result = (string)item;
			else if (item is Enum)
				result = item.GetType().FullName + "_" + item;
			else if (item is Type)
				result = item.ToString();
			// TODO Work with stream
			else
				result = JsonConvert.ToString(item);

			if (maxLength == -1 || result.Length <= maxLength)
				return result;
			return GetHashCode(result);
		}

		private static string GetHashCode(string input)
		{
			if (string.IsNullOrEmpty(input))
				return string.Empty;
			using SHA256 sha = SHA256.Create();
			byte[] textData = Encoding.UTF8.GetBytes(input);
			byte[] hash = sha.ComputeHash(textData);
			return BitConverter.ToString(hash).Replace("-", string.Empty);
		}
	}

	public abstract class CachedServiceBase<T> : CachedServiceBase
		where T : class
	{
		public T NonCached { get; }

		public CachedServiceBase(NonCached<T> nonCached)
		{
			this.NonCached = nonCached?.Instance ?? throw new ArgumentNullException(nameof(nonCached));
		}
	}
}
