namespace Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Microsoft.Extensions.Caching.Distributed;
	using Microsoft.Extensions.Caching.MongoDB;
	using MongoDB.Driver;
	using NUnit.Framework;
	using static NUnit.Framework.AssertionHelper;

	public class ExpiredEntriesCleanerTests : IntegrationTestsBase
	{
		[Test]
		public void Cleanup_AbsoluteExpiredEntry_IsRemoved()
		{
			var cleaner = new ExpiredEntriesCleaner(Clock, Options);
			var expiresIn10Seconds = new DistributedCacheEntryOptions()
				.SetAbsoluteExpiration(Clock.UtcNow.AddSeconds(10));
			Cache.SetString("key", "value", expiresIn10Seconds);

			Clock.Advance(TimeSpan.FromSeconds(5));
			cleaner.Run();
			Expect(GetEntryKeys(), Is.EqualTo(new[] {"key"}),
				"Clean before expiration doesn't remove entry");

			Clock.Advance(TimeSpan.FromSeconds(20));
			cleaner.Run();
			Expect(GetEntryKeys(), Is.Empty,
				"Clean after expiration removes entry");
		}

		[Test]
		public void Cleanup_NoExpiry_NotRemoved()
		{
			var cleaner = new ExpiredEntriesCleaner(Clock, Options);
			Cache.SetString("key", "value");

			cleaner.Run();

			Expect(GetEntryKeys(), Is.EqualTo(new[] {"key"}));
		}

		[Test]
		public void Cleanup_SlidingExpiredEntry_IsRemovedOnlyAfterExpiration()
		{
			var cleaner = new ExpiredEntriesCleaner(Clock, Options);
			var sliding10Seconds = new DistributedCacheEntryOptions()
				.SetSlidingExpiration(TimeSpan.FromSeconds(10));
			Cache.SetString("key", "value", sliding10Seconds);

			Clock.Advance(TimeSpan.FromSeconds(5));
			cleaner.Run();
			Expect(GetEntryKeys(), Is.EqualTo(new[] {"key"}),
				"Clean before expiration doesn't remove entry");

			Clock.Advance(TimeSpan.FromSeconds(20));
			cleaner.Run();
			Expect(GetEntryKeys(), Is.Empty,
				"Clean after expiration removes entry");
		}

		private List<string> GetEntryKeys()
		{
			return Collection.AsQueryable()
				.Select(e => e.Key)
				.ToList();
		}
	}
}