namespace Microsoft.Extensions.Caching.MongoDB
{
	using System;
	using Distributed;
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
		public static CacheEntry Create(ISystemClock clock, string key, byte[] value, DistributedCacheEntryOptions options)
		{
			var entry = new CacheEntry
			{
				Key = key,
				Value = value,
				SlidingDuration = options.SlidingExpiration,
				LastAccessedAt = clock.UtcNow
			};
			// note: no contract that I can find specifies precedence when both are set
			if (options.AbsoluteExpiration.HasValue)
			{
				entry.AbsolutionExpiration = options.AbsoluteExpiration.Value;
			}
			if (options.AbsoluteExpirationRelativeToNow.HasValue)
			{
				entry.AbsolutionExpiration = entry.LastAccessedAt.Add(options.AbsoluteExpirationRelativeToNow.Value);
			}
			return entry;
		}

		/// <summary>
		///     Document's _id = key, thus the key is the primary key in the DB
		///     and has a unique index built by default
		/// </summary>
		[BsonId]
		public string Key { get; set; }

		public byte[] Value { get; set; }

		public DateTimeOffset? AbsolutionExpiration { get; set; }

		public TimeSpan? SlidingDuration { get; set; }

		public DateTimeOffset LastAccessedAt { get; set; }

		public bool IsExpired(ISystemClock clock)
		{
			if (AbsolutionExpiration.HasValue
			    && AbsolutionExpiration <= clock.UtcNow)
			{
				return true;
			}
			if (SlidingDuration.HasValue
			    && LastAccessedAt.Add(SlidingDuration.Value) <= clock.UtcNow)
			{
				return true;
			}

			return false;
		}

		public void Refresh(ISystemClock clock)
		{
			if (IsExpired(clock))
			{
				return;
			}
			LastAccessedAt = clock.UtcNow;
		}
	}
}