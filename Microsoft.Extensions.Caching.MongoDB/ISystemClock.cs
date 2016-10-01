namespace Microsoft.Extensions.Caching.MongoDB
{
	using System;

	public interface ISystemClock
	{
		/// <summary>
		///     Retrieves the current system time in UTC.
		/// </summary>
		DateTime UtcNow { get; }
	}

	public class SystemClock : ISystemClock
	{
		public DateTime UtcNow => DateTime.UtcNow;
	}
}