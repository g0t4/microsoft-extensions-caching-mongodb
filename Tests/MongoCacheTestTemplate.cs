namespace Tests
{
	using System.Threading.Tasks;
	using Microsoft.Extensions.Caching.Distributed;
	using Microsoft.Extensions.Caching.MongoDB;
	using MongoDB.Driver;
	using NUnit.Framework;
	using static NUnit.Framework.AssertionHelper;

	public abstract class MongoCacheTestTemplate 
	{
		private const string TestConnectionString = "mongodb://localhost/caching-tests";
		protected MongoCache Cache;

		[SetUp]
		public void BeforeEachTest()
		{
			var client = new MongoClient();
			client.DropDatabase(new MongoUrl(TestConnectionString).DatabaseName);
			Cache = CreateMongoCache();
		}

		protected static MongoCache CreateMongoCache()
		{
			var options = new MongoCacheOptions
			{
				ConnectionString = TestConnectionString,
				CollectionName = "cache"
			};
			return new MongoCache(options);
		}

		[Test]
		public async Task Get_NoCachedValues_ReturnsNull()
		{
			var value = await Get("key");

			Expect(value, Is.Null, "Get");
		}


		[Test]
		public async Task Set_WithoutExpiration_StoresCacheEntry()
		{
			await Set("key", "value");

			Expect(await Get("key"), Is.EqualTo("value"));
		}


		[Test]
		public async Task Set_ExistingKey_Replaces()
		{
			await Set("key", "value");

			await Set("key", "replaced");

			Expect(await Get("key"), Is.EqualTo("replaced"));
		}


		[Test]
		public async Task Remove_WithoutEntry_DoesNothing()
		{
			await Remove("key");
		}

		[Test]
		public async Task Remove_WithEntry_Removes()
		{
			await Set("key", "value");

			await Remove("key");

			Expect(await Get("key"), Is.Null);
		}


// todo Set needs to validate expiration options - ignore if absolute < now 
// todo set needs to set expiration
// todo Get needs to check expiration
// todo Get needs to purge if expired
// todo refresh just needs to do a Get and ignore the value
// todo do I want some optional feature to purge the cache on a background thread like MSSQL? Items might otherwise expire and just accumulate, maybe some record count or time period for purging? configurable too
// todo validate key not null on all methods, and value not null on set, in some of these we can ignore null key (like refresh and delete) - or maybe not, need to see if docs have expected behavior of API
// todo options passed can have absolute time or absolute relative to now, parse both


		protected abstract Task<string> Get(string key);
		protected abstract Task Set(string key, string value);
		protected abstract Task Remove(string key);
	}

	public class MongoCacheSynchronousTests : MongoCacheTestTemplate
	{
		protected override async Task<string> Get(string key)
			=> Cache.GetString(key);

		protected override async Task Set(string key, string value)
			=> Cache.SetString(key, value);

		protected override async Task Remove(string key)
			=> Cache.Remove(key);
	}


	public class MongoCacheAsynchronousTests : MongoCacheTestTemplate
	{
		protected override Task<string> Get(string key)
			=> Cache.GetStringAsync(key);

		protected override Task Set(string key, string value)
			=> Cache.SetStringAsync(key, value);

		protected override Task Remove(string key)
			=> Cache.RemoveAsync(key);
	}
}