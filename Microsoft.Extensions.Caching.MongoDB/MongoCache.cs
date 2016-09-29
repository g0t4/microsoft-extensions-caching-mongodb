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

		// todo extension method to register services

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

			var options = optionsAccessor.Value;
			if (options.ConnectionString == null)
			{
				throw new ArgumentException("ConnectionString is missing", nameof(optionsAccessor));
			}

			var url = new MongoUrl(options.ConnectionString);
			if (url.DatabaseName == null)
			{
				throw new ArgumentException("ConnectionString requires a database name", nameof(optionsAccessor));
			}

			var client = new MongoClient(url);
			_Collection = client.GetDatabase(url.DatabaseName)
				.GetCollection<CacheEntry>(options.CollectionName);
		}

		public byte[] Get(string key)
		{
			var entry = _Collection.Find(e => e.Key == key).FirstOrDefault();
			return GetCommon(entry);
		}

		private byte[] GetCommon(CacheEntry entry)
		{
			if (entry == null)
			{
				return null;
			}
			if (entry.IsExpired(_Clock))
			{
				return null;
			}
			return entry.Value;
		}

		public async Task<byte[]> GetAsync(string key)
		{
			var entry = await _Collection.Find(e => e.Key == key).FirstOrDefaultAsync();
			return GetCommon(entry);
		}

		public void Refresh(string key)
		{
			throw new NotImplementedException();
		}

		public Task RefreshAsync(string key)
		{
			throw new NotImplementedException();
		}

		public void Remove(string key)
		{
			_Collection.DeleteOne(e => e.Key == key);
			// todo confirm?
		}

		public Task RemoveAsync(string key)
		{
			return _Collection.DeleteOneAsync(e => e.Key == key);
			// todo confirm?
		}

		public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			var entry = CacheEntry.Create(_Clock, key, value, options);
			if (entry.IsExpired(_Clock))
			{
				return;
			}

			_Collection.ReplaceOne(e => e.Key == key, entry, new UpdateOptions {IsUpsert = true});
		}

		public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			var entry = CacheEntry.Create(_Clock, key, value, options);
			if (entry.IsExpired(_Clock))
			{
				return Task.FromResult(0);
			}
			return _Collection.ReplaceOneAsync(e => e.Key == key, entry, new UpdateOptions {IsUpsert = true});
		}
	}
}