namespace Microsoft.Extensions.Caching.MongoDB
{
	using System;
	using global::MongoDB.Bson.Serialization.Attributes;

	public class CacheEntry
	{
		/// <summary>
		///     Document's _id = key, thus the key is the primary key in the DB
		///     and has a unique index built by default
		/// </summary>
		[BsonId]
		public string Key { get; set; }

		public byte[] Value { get; set; }
		public DateTimeOffset? AbsolutionExpiration { get; set; }
		public TimeSpan? SlidingExpiration { get; set; }
	}
}