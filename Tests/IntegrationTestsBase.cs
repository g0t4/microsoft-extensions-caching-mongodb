namespace Tests
{
	using Microsoft.Extensions.Caching.MongoDB;
	using MongoDB.Driver;
	using NUnit.Framework;

	public abstract class IntegrationTestsBase
	{
		protected MongoCache Cache;
		protected TestClock Clock;
		private const string TestConnectionString = "mongodb://localhost/caching-tests";
		protected IMongoCollection<CacheEntry> Collection;
		protected MongoCacheOptions Options;

		[SetUp]
		public void BeforeEachTest()
		{
			var client = new MongoClient();
			var url = new MongoUrl(TestConnectionString);
			client.DropDatabase(url.DatabaseName);
			Options = new MongoCacheOptions
			{
				ConnectionString = TestConnectionString,
				CollectionName = "cache",
				WaitForRefreshOnGet = true
			};
			Collection = client.GetDatabase(url.DatabaseName)
				.GetCollection<CacheEntry>("cache");
			Clock = new TestClock();
			Cache = new MongoCache(Clock, Options);
		}
	}
}