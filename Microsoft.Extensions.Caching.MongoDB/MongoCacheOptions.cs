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

		MongoCacheOptions IOptions<MongoCacheOptions>.Value => this;
	}
}