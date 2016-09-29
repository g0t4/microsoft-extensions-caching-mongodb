namespace Tests
{
	using System.Threading.Tasks;
	using Microsoft.Extensions.Caching.Distributed;
	using Microsoft.Extensions.Caching.MongoDB;
	using MongoDB.Driver;
	using NUnit.Framework;
	using static NUnit.Framework.AssertionHelper;

	public class MongoCacheTests
	{
		private const string TestConnectionString = "mongodb://localhost/caching-tests";

		[SetUp]
		public void BeforeEachTest()
		{
			var client = new MongoClient();
			client.DropDatabase(new MongoUrl(TestConnectionString).DatabaseName);
		}

		[Test]
		public async Task Get_NoCachedValues_ReturnsNull()
		{
			var cache = CreateMongoCache();

			var value = cache.Get("key");
			Expect(value, Is.Null, "Get");

			var asyncValue = await cache.GetAsync("key");
			Expect(asyncValue, Is.Null, "GetAsync");
		}

		private static MongoCache CreateMongoCache()
		{
			var options = new MongoCacheOptions
			{
				ConnectionString = TestConnectionString,
				CollectionName = "cache"
			};
			return new MongoCache(options);
		}

		[Test]
		public void Set_WithoutExpiration_StoresCacheEntry()
		{
			var cache = CreateMongoCache();

			cache.SetString("key", "value");

			Expect(cache.GetString("key"), Is.EqualTo("value"));
		}

		
	}
}