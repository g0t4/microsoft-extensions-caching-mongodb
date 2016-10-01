namespace Tests
{
	using System;
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
		protected TestClock Clock;

		[SetUp]
		public void BeforeEachTest()
		{
			var client = new MongoClient();
			client.DropDatabase(new MongoUrl(TestConnectionString).DatabaseName);
			var options = new MongoCacheOptions
			{
				ConnectionString = TestConnectionString,
				CollectionName = "cache",
				WaitForRefreshOnGet = true
			};
			Clock = new TestClock();
			Cache = new MongoCache(Clock, options);
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
		public async Task Get_ExpiringEntry_ReturnsItUntilExpired()
		{
			// note: more tests of expiration scenarios in CacheEntry type, just an integration test here

			var options = new DistributedCacheEntryOptions()
				.SetAbsoluteExpiration(Clock.UtcNow.AddSeconds(1));
			await Set("key", "value", options);

			Expect(await Get("key"), Is.EqualTo("value"), "Entry should be accessible before it expires");

			Clock.Advance(TimeSpan.FromSeconds(1));
			Expect(await Get("key"), Is.Null, "Entry should not be accessible if it expired");
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

		[Test]
		public async Task Set_AlreadyExpired_Ignores()
		{
			var expiresNow = new DistributedCacheEntryOptions()
				.SetAbsoluteExpiration(Clock.UtcNow);

			await Set("key", "value", expiresNow);

			Expect(await Get("key"), Is.Null);
		}

		[Test]
		public async Task Refresh_ExtendsLifeWhenInsideWindow()
		{
			var slidingOptions = new DistributedCacheEntryOptions()
				.SetSlidingExpiration(TimeSpan.FromSeconds(10));
			await Set("key", "value", slidingOptions);

			var withinWindow = TimeSpan.FromSeconds(5);
			Clock.Advance(withinWindow);

			await Refresh("key"); // should extend life to 15 seconds after creation

			var withinSecondWindow = TimeSpan.FromSeconds(9);
			Clock.Advance(withinSecondWindow); // 14 seconds after creating entry
			Expect(await Get("key"), Is.EqualTo("value"), "Should be accessible within second window");

			// Get also refreshes, so we need to jump at least 10 full seconds past a Get to be at end of window
			Clock.Advance(TimeSpan.FromSeconds(20));
			await Refresh("key"); // should not extend life
			Expect(await Get("key"), Is.Null, "Should not be accessible after Refresh outside window");
		}

		[Test]
		public async Task Get_ExtendsLifeWhenInsideWindow()
		{
			var slidingOptions = new DistributedCacheEntryOptions()
				.SetSlidingExpiration(TimeSpan.FromSeconds(10));
			await Set("key", "value", slidingOptions);

			var withinWindow = TimeSpan.FromSeconds(5);
			Clock.Advance(withinWindow);

			await Get("key"); // should extend life to 15 seconds after creation

			var withinSecondWindow = TimeSpan.FromSeconds(9);
			Clock.Advance(withinSecondWindow); // 14 seconds after creating entry
			Expect(await Get("key"), Is.EqualTo("value"), "Should be accessible within second window");

			Clock.Advance(TimeSpan.FromSeconds(20));
			Expect(await Get("key"), Is.Null, "Should not be accessible after window");
		}

// todo Get needs to purge if expired
// todo do I want some optional feature to purge the cache on a background thread like MSSQL? Items might otherwise expire and just accumulate, maybe some record count or time period for purging? configurable too

		protected abstract Task<string> Get(string key);
		protected abstract Task Set(string key, string value);
		protected abstract Task Set(string key, string value, DistributedCacheEntryOptions options);
		protected abstract Task Remove(string key);
		protected abstract Task Refresh(string key);
	}

	public class MongoCacheSynchronousTests : MongoCacheTestTemplate
	{
		protected override async Task<string> Get(string key)
			=> Cache.GetString(key);

		protected override async Task Set(string key, string value)
			=> Cache.SetString(key, value);

		protected override async Task Set(string key, string value, DistributedCacheEntryOptions options)
			=> Cache.SetString(key, value, options);

		protected override async Task Remove(string key)
			=> Cache.Remove(key);

		protected override async Task Refresh(string key)
			=> Cache.Refresh(key);
	}


	public class MongoCacheAsynchronousTests : MongoCacheTestTemplate
	{
		protected override Task<string> Get(string key)
			=> Cache.GetStringAsync(key);

		protected override Task Set(string key, string value)
			=> Cache.SetStringAsync(key, value);

		protected override Task Set(string key, string value, DistributedCacheEntryOptions options)
			=> Cache.SetStringAsync(key, value, options);

		protected override Task Remove(string key)
			=> Cache.RemoveAsync(key);

		protected override Task Refresh(string key)
			=> Cache.RefreshAsync(key);
	}
}