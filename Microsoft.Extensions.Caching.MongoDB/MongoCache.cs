namespace Microsoft.Extensions.Caching.MongoDB
{
	using System;
	using System.Threading.Tasks;
	using Distributed;
	using global::MongoDB.Driver;
	using Options;

	public class MongoCache : IDistributedCache
	{
		private readonly IMongoCollection<CacheEntry> _Collection;

		public MongoCache(IOptions<MongoCacheOptions> optionsAccessor)
		{
			if (optionsAccessor == null)
			{
				throw new ArgumentNullException(nameof(optionsAccessor));
			}

			if (optionsAccessor.Value.ConnectionString == null)
			{
				throw new ArgumentException("ConnectionString is missing", nameof(optionsAccessor));
			}

			var url = new MongoUrl(optionsAccessor.Value.ConnectionString);
			if (url.DatabaseName == null)
			{
				throw new ArgumentException("ConnectionString requires a database name", nameof(optionsAccessor));
			}

			var client = new MongoClient(url);
			_Collection = client.GetDatabase(url.DatabaseName)
				.GetCollection<CacheEntry>(optionsAccessor.Value.CollectionName);
		}

		public byte[] Get(string key)
		{
			var entry = _Collection.Find(e => e.Key == key).FirstOrDefault();
			return entry?.Value;
		}

		public Task<byte[]> GetAsync(string key)
		{
			return Task.FromResult<byte[]>(null);
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
			throw new NotImplementedException();
		}

		public Task RemoveAsync(string key)
		{
			throw new NotImplementedException();
		}

		public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			var entry = new CacheEntry
			{
				Key = key,
				Value = value
			};
			_Collection.InsertOne(entry);
		}

		public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			throw new NotImplementedException();
		}
	}
}