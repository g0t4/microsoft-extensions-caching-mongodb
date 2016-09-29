namespace Tests
{
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
		public void IsExpired_ExpiresNow_ReturnsTrue()
		{
			var clock = new TestClock();
			var entry = new CacheEntry {AbsolutionExpiration = clock.UtcNow};

			Expect(entry.IsExpired(clock), Is.True);
		}

		[Test]
		public void IsExpired_ExpiresLater_ReturnsFalse()
		{
			var clock = new TestClock();
			var entry = new CacheEntry {AbsolutionExpiration = clock.UtcNow.AddSeconds(1)};

			Expect(entry.IsExpired(clock), Is.False);
		}
	}
}