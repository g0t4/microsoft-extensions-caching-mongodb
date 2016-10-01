namespace Microsoft.Extensions.Caching.MongoDB
{
	using System;
	using global::MongoDB.Driver;
	using Options;

	public class MongoCacheOptions : IOptions<MongoCacheOptions>
	{
		/// <summary>
		///     The connection string to mongodb, including a database name.
		///     i.e. mongodb://localhost/databaseName
		/// </summary>
		public string ConnectionString { get; set; }

		/// <summary>
		///     If configured, remove expired items in the background.
		/// </summary>
		public TimeSpan? ExpiredItemsDeletionInterval { get; set; }

		public string CollectionName { get; set; } = "cacheEntries";

		/// <summary>
		///     Whether or not a call to Get synchronously or asynchronously refreshes LastAccessedAt
		/// </summary>
		public bool WaitForRefreshOnGet { get; set; }

		MongoCacheOptions IOptions<MongoCacheOptions>.Value => this;

		public virtual IMongoCollection<CacheEntry> GetCacheEntryCollection()
		{
			if (ConnectionString == null)
			{
				throw new ArgumentException("ConnectionString is missing");
			}

			var url = new MongoUrl(ConnectionString);
			if (url.DatabaseName == null)
			{
				throw new ArgumentException("ConnectionString requires a database name");
			}

			var client = new MongoClient(url);
			return client.GetDatabase(url.DatabaseName)
				.GetCollection<CacheEntry>(CollectionName);
		}
	}
}