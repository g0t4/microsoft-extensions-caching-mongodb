namespace Microsoft.Extensions.Caching.MongoDB
{
	using Options;

	public class MongoCacheOptions : IOptions<MongoCacheOptions>
	{
		/// <summary>
		///     The connection string to mongodb, including a database name.
		///     i.e. mongodb://localhost/databaseName
		/// </summary>
		public string ConnectionString { get; set; }

		// todo add ExpiredItemsDeletionInterval like MSSQL version, make it optional?
		// todo add DefaultSlidingExpiration like MSSQL version
		public string CollectionName { get; set; } = "cacheEntries";

		/// <summary>
		///     Whether or not a call to Get synchronously or asynchronously refreshes LastAccessedAt
		/// </summary>
		public bool WaitForRefreshOnGet { get; set; }

		MongoCacheOptions IOptions<MongoCacheOptions>.Value => this;
	}
}