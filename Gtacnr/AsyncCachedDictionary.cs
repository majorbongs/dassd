using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gtacnr;

public class AsyncCachedDictionary<TKey, TValue>
{
	private struct CacheItem
	{
		public DateTime RetreivedTime;

		public TValue Value;
	}

	private Dictionary<TKey, CacheItem> _cache = new Dictionary<TKey, CacheItem>();

	private TimeSpan _timeout;

	private readonly object _lock = new object();

	public AsyncCachedDictionary(TimeSpan timeout)
	{
		_timeout = timeout;
	}

	public async Task<TValue> GetOrResolveAsync(TKey key, Func<TKey, Task<TValue>> resolve)
	{
		lock (_lock)
		{
			if (_cache.ContainsKey(key))
			{
				_cache[key].RetreivedTime.Add(DateTime.UtcNow - _cache[key].RetreivedTime);
				return _cache[key].Value;
			}
		}
		TValue value = await resolve(key);
		await Utils.BackToMainThread();
		lock (_lock)
		{
			_cache[key] = new CacheItem
			{
				RetreivedTime = DateTime.UtcNow,
				Value = value
			};
			return value;
		}
	}

	public void Set(TKey key, TValue value)
	{
		lock (_lock)
		{
			_cache[key] = new CacheItem
			{
				RetreivedTime = DateTime.UtcNow,
				Value = value
			};
		}
	}

	public void Cleanup()
	{
		lock (_lock)
		{
			DateTime utcNow = DateTime.UtcNow;
			foreach (TKey item in _cache.Keys.ToList())
			{
				if (_cache[item].RetreivedTime.Add(_timeout) < utcNow)
				{
					_cache.Remove(item);
				}
			}
		}
	}
}
