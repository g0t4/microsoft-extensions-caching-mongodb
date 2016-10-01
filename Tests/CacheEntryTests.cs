namespace Tests
{
	using System;
	using Microsoft.Extensions.Caching.Distributed;
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
		public void Create_MapsKey_Value_AbsoluteExpiration_AndSlidingDuration()
		{
			var expiration = new DateTime(2016, 10, 1, 0, 0, 0, DateTimeKind.Utc);
			var window = TimeSpan.FromSeconds(2);
			var options = new DistributedCacheEntryOptions()
				.SetAbsoluteExpiration(expiration)
				.SetSlidingExpiration(window);

			var entry = CacheEntry.Create(new TestClock(), "key", new byte[1], options);

			Expect(entry.Key, Is.EqualTo("key"));
			Expect(entry.Value, Is.EqualTo(new byte[1]));
			Expect(entry.ExpiresAt, Is.EqualTo(expiration));
			Expect(entry.SlidingDuration, Is.EqualTo(window));
		}

		[Test]
		public void Create_WithSlidingExpiration_SetsRefreshBefore()
		{
			var options = new DistributedCacheEntryOptions()
				.SetSlidingExpiration(TimeSpan.FromSeconds(10));
			var clock = new TestClock();

			var entry = CacheEntry.Create(clock, "key", new byte[1], options);

			Expect(entry.RefreshBefore, Is.EqualTo(clock.UtcNow.AddSeconds(10)));
		}

		[Test]
		public void Create_WithoutSlidingExpiration_DoesNotSetRefreshBefore()
		{
			var options = new DistributedCacheEntryOptions();
			var clock = new TestClock();

			var entry = CacheEntry.Create(clock, "key", new byte[1], options);

			Expect(entry.RefreshBefore, Is.Null);
		}

		[Test]
		public void Create_WithAbsoluteRelativeToNow_ComputesAbsoluteBasedOnNow()
		{
			var options = new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
			};
			var clock = new TestClock();

			var entry = CacheEntry.Create(clock, "key", new byte[1], options);

			Expect(entry.ExpiresAt, Is.EqualTo(clock.UtcNow.AddSeconds(10)));
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
				ExpiresAt = clock.UtcNow.AddSeconds(10)
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
			var entry = Sliding10SecondEntry(clock);

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

		private static CacheEntry Sliding10SecondEntry(ISystemClock clock) =>
			new CacheEntry
			{
				SlidingDuration = TimeSpan.FromSeconds(10),
				RefreshBefore = clock.UtcNow.Add(TimeSpan.FromSeconds(10))
			};

		[Test]
		public void IsExpired_SlideUntilTimeIsBeforeWindowEnds_ExpiresAtAbsoluteExpirationTime()
		{
			var clock = new TestClock();
			var absoluteExpirationWithinWindow = clock.UtcNow.AddSeconds(5);
			var entry = Sliding10SecondEntry(clock);
			entry.ExpiresAt = absoluteExpirationWithinWindow;

			Expect(entry.IsExpired(clock), Is.False, "Does not immediately expire");

			clock.UtcNow = absoluteExpirationWithinWindow;

			Expect(entry.IsExpired(clock), Is.True, "Should expire at absolute time within the window");
		}

		[Test]
		public void IsExpired_SlideUntilTimeIsAfterWindowEnds_ExpiresAtEndOfWindow()
		{
			var clock = new TestClock();
			var absoluteExpirationAfterWindow = clock.UtcNow.AddSeconds(15);
			var entry = Sliding10SecondEntry(clock);
			entry.ExpiresAt = absoluteExpirationAfterWindow;

			Expect(entry.IsExpired(clock), Is.False, "Does not immediately expire");

			clock.Advance(TimeSpan.FromSeconds(10));

			Expect(entry.IsExpired(clock), Is.True, "Should expire at end of window");
		}

		[Test]
		public void Refresh_NoSlidingExpiration_DoesNotSetRefreshBefore()
		{
			var clock = new TestClock();
			var entry = new CacheEntry();

			entry.Refresh(clock);

			Expect(entry.RefreshBefore, Is.Null);
		}

		[Test]
		public void Refresh_WithSlidingExpiration_OnlyRefreshesWithinWindow()
		{
			var now = new DateTime(2016, 10, 1, 0, 0, 0, DateTimeKind.Utc);
			var clock = new TestClock {UtcNow = now};
			var entry = Sliding10SecondEntry(clock);

			var withinWindow = TimeSpan.FromSeconds(1);
			clock.Advance(withinWindow);
			entry.Refresh(clock);
			Expect(entry.RefreshBefore, Is.EqualTo(now.AddSeconds(11)), "Should refresh within window.");

			var toEndOfWindow = TimeSpan.FromSeconds(10);
			clock.Advance(toEndOfWindow);
			entry.Refresh(clock);
			Expect(entry.RefreshBefore, Is.EqualTo(now.AddSeconds(11)), "Should not refresh at end of window.");
		}
	}
}