namespace Microsoft.Extensions.Caching.MongoDB
{
	using System;
	using System.Threading.Tasks;
	using Distributed;
	using Options;

	public class MongoCache : IDistributedCache
	{
		public MongoCache(IOptions<MongoCacheOptions> optionsAccessor)
		{
			
		}
		public byte[] Get(string key)
		{
			throw new NotImplementedException();
		}

		public Task<byte[]> GetAsync(string key)
		{
			throw new NotImplementedException();
		}

		public void Refresh(string key)
		{
			throw new NotImplementedException();
		}

		public Task RefreshAsync(string key)
		{
			throw new NotImplementedException();
		}

		public void Remove(string key)
		{
			throw new NotImplementedException();
		}

		public Task RemoveAsync(string key)
		{
			throw new NotImplementedException();
		}

		public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			throw new NotImplementedException();
		}

		public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			throw new NotImplementedException();
		}
	}
}