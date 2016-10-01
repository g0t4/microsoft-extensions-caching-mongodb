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
			var now = clock.UtcNow;
			var slidingDuration = options.SlidingExpiration;
			var entry = new CacheEntry
			{
				Key = key,
				Value = value,
				SlidingDuration = slidingDuration
			};
			if (slidingDuration.HasValue)
			{
				entry.RefreshBefore = now.Add(slidingDuration.Value);
			}
			// note: no contract that I can find specifies precedence when both are set
			if (options.AbsoluteExpiration.HasValue)
			{
				entry.ExpiresAt = options.AbsoluteExpiration.Value.UtcDateTime;
			}
			if (options.AbsoluteExpirationRelativeToNow.HasValue)
			{
				entry.ExpiresAt = now.Add(options.AbsoluteExpirationRelativeToNow.Value);
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

		public DateTime? ExpiresAt { get; set; }

		public TimeSpan? SlidingDuration { get; set; }

		public DateTime? RefreshBefore { get; set; }

		public bool IsExpired(ISystemClock clock)
		{
			if (ExpiresAt.HasValue
			    && ExpiresAt <= clock.UtcNow)
			{
				return true;
			}
			if (RefreshBefore.HasValue
			    && RefreshBefore <= clock.UtcNow)
			{
				return true;
			}

			return false;
		}

		public void Refresh(ISystemClock clock)
		{
			if (IsExpired(clock)
			    || !SlidingDuration.HasValue)
			{
				return;
			}

			RefreshBefore = clock.UtcNow.Add(SlidingDuration.Value);
		}
	}
}