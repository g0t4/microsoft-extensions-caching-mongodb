namespace Microsoft.Extensions.Caching.MongoDB
{
	using global::MongoDB.Driver;
	using Options;

	public class ExpiredEntriesCleaner
	{
		private readonly ISystemClock _Clock;
		private readonly IMongoCollection<CacheEntry> _Collection;

		public ExpiredEntriesCleaner(ISystemClock clock, IOptions<MongoCacheOptions> optionsAccessor)
		{
			_Clock = clock;
			_Collection = optionsAccessor.Value.GetCacheEntryCollection();
		}

		public void Run()
		{
			_Collection.DeleteMany(e => e.ExpiresAt <= _Clock.UtcNow
			                            || e.RefreshBefore <= _Clock.UtcNow);
		}
	}
}