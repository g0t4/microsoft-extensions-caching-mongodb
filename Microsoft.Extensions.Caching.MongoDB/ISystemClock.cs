namespace Microsoft.Extensions.Caching.MongoDB
{
	using System;

	public interface ISystemClock
	{
		/// <summary>
		///     Retrieves the current system time in UTC.
		/// </summary>
		DateTimeOffset UtcNow { get; }
	}

	public class SystemClock : ISystemClock
	{
		public DateTimeOffset UtcNow => DateTime.UtcNow;
	}
}