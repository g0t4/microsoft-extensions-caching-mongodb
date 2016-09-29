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
		// todo default collection name
		public string CollectionName { get; set; }

		/// <summary>
		/// Whether or not a call to Get synchronously or asynchronously refreshes LastAccessedAt
		/// </summary>
		//public bool BlockForRefresh { get; set; }

		MongoCacheOptions IOptions<MongoCacheOptions>.Value => this;
	}
}