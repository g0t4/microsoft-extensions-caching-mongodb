namespace Tests
{
	using System;
	using Microsoft.Extensions.Caching.MongoDB;
	using MongoDB.Bson;
	using NUnit.Framework;
	using static NUnit.Framework.AssertionHelper;

	public class CacheEntryTests
	{
		[Test]
		public void CacheEntryKeySerializesToDocumentId()
		{
			var entry = new CacheEntry {Key = "key"};

			var serialized = entry.ToBsonDocument();

			Expect(serialized["_id"].AsString, Is.EqualTo("key"));
		}

		[Test]
		public void IsExpired_NoExpiration_ReturnsFalse()
		{
			var clock = new TestClock();
			var entry = new CacheEntry();

			Expect(entry.IsExpired(clock), Is.False);
		}

		[Test]
		public void IsExpired_AbsoluteOnly_ExpiresAtTheAbsoluteExpirationTime()
		{
			var clock = new TestClock();
			var entry = new CacheEntry
			{
				AbsolutionExpiration = clock.UtcNow.AddSeconds(10)
			};

			Expect(entry.IsExpired(clock), Is.False, "Absolute should not be expired before the expiration time");

			var toAbsoluteExpirationTime = TimeSpan.FromSeconds(10);
			clock.Advance(toAbsoluteExpirationTime);
			Expect(entry.IsExpired(clock), Is.True, "Absolute should be expired at the expiration time");

			var toAfterExpirationTime = TimeSpan.FromSeconds(1);
			clock.Advance(toAfterExpirationTime);
			Expect(entry.IsExpired(clock), Is.True, "Absolute should be expired after the expiration time");
		}

		[Test]
		public void IsExpired_SlidingOnly_ExpiresWhenWindowEnds()
		{
			var clock = new TestClock();
			var entry = new CacheEntry
			{
				SlidingExpiration = TimeSpan.FromSeconds(10),
				LastAccessedAt = clock.UtcNow
			};

			Expect(entry.IsExpired(clock), Is.False, "Sliding should not immediately expire");

			var withinWidow = TimeSpan.FromSeconds(5);
			clock.Advance(withinWidow);
			Expect(entry.IsExpired(clock), Is.False, "Sliding should not expire within window");

			var toEndOfWindow = TimeSpan.FromSeconds(5);
			clock.Advance(toEndOfWindow);
			Expect(entry.IsExpired(clock), Is.True, "Sliding should expire at end of window");

			var toAfterWindow = TimeSpan.FromSeconds(1);
			clock.Advance(toAfterWindow);
			Expect(entry.IsExpired(clock), Is.True, "Sliding should expire after the window");
		}

		[Test]
		public void IsExpired_SlideUntilTimeIsBeforeWindowEnds_ExpiresAtAbsoluteExpirationTime()
		{
			var clock = new TestClock();
			var absoluteExpirationWithinWindow = clock.UtcNow.AddSeconds(5);
			var entry = new CacheEntry
			{
				SlidingExpiration = TimeSpan.FromSeconds(10),
				LastAccessedAt = clock.UtcNow,
				AbsolutionExpiration = absoluteExpirationWithinWindow
			};

			Expect(entry.IsExpired(clock), Is.False, "Does not immediately expire");

			clock.UtcNow = absoluteExpirationWithinWindow;

			Expect(entry.IsExpired(clock), Is.True, "Should expire at absolute time within the window");
		}

		[Test]
		public void IsExpired_SlideUntilTimeIsAfterWindowEnds_ExpiresAtEndOfWindow()
		{
			var clock = new TestClock();
			var absoluteExpirationAfterWindow = clock.UtcNow.AddSeconds(15);
			var entry = new CacheEntry
			{
				SlidingExpiration = TimeSpan.FromSeconds(10),
				LastAccessedAt = clock.UtcNow,
				AbsolutionExpiration = absoluteExpirationAfterWindow
			};

			Expect(entry.IsExpired(clock), Is.False, "Does not immediately expire");

			clock.Advance(TimeSpan.FromSeconds(10));

			Expect(entry.IsExpired(clock), Is.True, "Should expire at end of window");
		}
	}
}