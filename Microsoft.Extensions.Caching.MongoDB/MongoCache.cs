namespace Microsoft.Extensions.Caching.MongoDB
{
	using System;
	using System.Threading.Tasks;
	using Distributed;
	using global::MongoDB.Driver;
	using Options;

	public class MongoCache : IDistributedCache
	{
		private readonly ISystemClock _Clock;
		private readonly IMongoCollection<CacheEntry> _Collection;
		private readonly MongoCacheOptions _Options;

		// todo extension method to register services, should validate config?
		// todo spin up cleaner?
		public MongoCache(ISystemClock clock, IOptions<MongoCacheOptions> optionsAccessor)
		{
			if (clock == null)
			{
				throw new ArgumentNullException(nameof(clock));
			}
			_Clock = clock;

			if (optionsAccessor == null)
			{
				throw new ArgumentNullException(nameof(optionsAccessor));
			}

			_Options = optionsAccessor.Value;
			_Collection = _Options.GetCacheEntryCollection();
		}

		public byte[] Get(string key)
		{
			return GetAndRefresh(key, _Options.WaitForRefreshOnGet);
		}

		/// <summary>
		///     note as ugly as it is, we need separate implementations of Sync & Async
		///     if we wrap either way async over sync, or sync over async, we take away decisions from consumers
		///     and take away flexibility
		///     MongoDB has separate APIs for sync & async so we're exposing that via these different implementations
		///     tests are parameterized so not a big deal, just need twice the impl
		/// </summary>
		/// <param name="key"></param>
		/// <param name="waitForRefresh">
		///     If false, refresh is fire and forget. If true, the value isn't returned until refresh
		///     completes
		/// </param>
		/// <returns></returns>
		private byte[] GetAndRefresh(string key, bool waitForRefresh)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			var entry = _Collection.Find(e => e.Key == key).FirstOrDefault();
			if (entry == null)
			{
				return null;
			}
			if (entry.IsExpired(_Clock))
			{
				TriggerCleanup(entry);
				return null;
			}
			entry.Refresh(_Clock);
			var refresh = Builders<CacheEntry>.Update
				.Set(e => e.RefreshBefore, entry.RefreshBefore);
			if (waitForRefresh)
			{
				_Collection.UpdateOne(e => e.Key == key, refresh);
			}
			else
			{
				_Collection.UpdateOneAsync(e => e.Key == key, refresh);
			}
			return entry.Value;
		}

		private void TriggerCleanup(CacheEntry entry)
		{
			// todo remove old entries
		}

		public async Task<byte[]> GetAsync(string key)
		{
			return await GetAndRefreshAsync(key, _Options.WaitForRefreshOnGet);
		}

		private async Task<byte[]> GetAndRefreshAsync(string key, bool waitForRefresh)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			var entry = await _Collection.Find(e => e.Key == key).FirstOrDefaultAsync();
			if (entry == null)
			{
				return null;
			}
			if (entry.IsExpired(_Clock))
			{
				TriggerCleanup(entry);
				return null;
			}
			entry.Refresh(_Clock);
			var refresh = Builders<CacheEntry>.Update
				.Set(e => e.RefreshBefore, entry.RefreshBefore);
			if (waitForRefresh)
			{
				await _Collection.UpdateOneAsync(e => e.Key == entry.Key, refresh);
			}
			else
			{
#pragma warning disable 4014
				_Collection.UpdateOneAsync(e => e.Key == entry.Key, refresh);
#pragma warning restore 4014
			}
			return entry.Value;
		}

		/// <summary>
		///     Referesh always waits for save to DB, use this if you want to be sure that the update on a refresh is persisted.
		/// </summary>
		/// <param name="key"></param>
		public void Refresh(string key) => GetAndRefresh(key, waitForRefresh: true);

		/// <summary>
		///     Refresh always awaits the save to DB, use this if you want to be sure that the update on a refresh is persisted.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public Task RefreshAsync(string key) => GetAndRefreshAsync(key, waitForRefresh: true);

		public void Remove(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			_Collection.DeleteOne(e => e.Key == key);
		}

		public Task RemoveAsync(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			// we could check delete result, but what should we do with it? 
			// let's say we call remove and no items match, is that really a problem, not IMO.
			// net result is the same, the item is gone.
			return _Collection.DeleteOneAsync(e => e.Key == key);
		}

		public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
			if (value == null)
			{
				// todo low - call Remove?
				throw new ArgumentNullException(nameof(key));
			}
			options = options ?? new DistributedCacheEntryOptions();

			var entry = CacheEntry.Create(_Clock, key, value, options);
			if (entry.IsExpired(_Clock))
			{
				return;
			}

			_Collection.ReplaceOne(e => e.Key == key, entry, new UpdateOptions {IsUpsert = true});
		}

		public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
			if (value == null)
			{
				// todo low - call Remove?
				throw new ArgumentNullException(nameof(key));
			}
			options = options ?? new DistributedCacheEntryOptions();

			var entry = CacheEntry.Create(_Clock, key, value, options);
			if (entry.IsExpired(_Clock))
			{
				return Task.FromResult(0);
			}
			return _Collection.ReplaceOneAsync(e => e.Key == key, entry, new UpdateOptions {IsUpsert = true});
		}
	}
}