namespace Tests
{
	using System.Threading.Tasks;
	using Microsoft.Extensions.Caching.MongoDB;
	using NUnit.Framework;
	using static NUnit.Framework.AssertionHelper;

	public class MongoCacheTests
	{
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
				ConnectionString = "mongodb://localhost/caching-tests",
				CollectionName = "cache"
			};
			return new MongoCache(options);
		}

		[Test]
		public void Set_StoresCacheEntry()
		{
		}
	}
}