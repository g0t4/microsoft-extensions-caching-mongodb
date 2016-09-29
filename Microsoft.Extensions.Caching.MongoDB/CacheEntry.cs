namespace Microsoft.Extensions.Caching.MongoDB
{
	using System;
	using global::MongoDB.Bson.Serialization.Attributes;

	/// <summary>
	///     Four possible expiration types:
	///     - None (neither absolute nor sliding are set) - lives until removed
	///     - Absolute Only (sliding is not set) - lives until specific time
	///     - Sliding Only (absolute not set) - lives for duration of sliding window, extends life if accessed before current
	///     window ends (last access + sliding window duration)
	///     - Slide Until (sliding and absolute are both set) - like Sliding Only with an absolute end to the sliding
	/// </summary>
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

		public DateTimeOffset LastAccessedAt { get; set; }

		public bool IsExpired(ISystemClock clock)
		{
			return AbsolutionExpiration.HasValue 
				&& AbsolutionExpiration <= clock.UtcNow;
		}
	}
}