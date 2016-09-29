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
	}
}