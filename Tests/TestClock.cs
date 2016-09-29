namespace Tests
{
	using System;
	using Microsoft.Extensions.Caching.MongoDB;

	public class TestClock : ISystemClock
	{
		public DateTimeOffset UtcNow { get; set; } = new DateTime(2010, 1, 1);

		public void Advance(TimeSpan timeSpan)
		{
			UtcNow = UtcNow.Add(timeSpan);
		}

		public void Later()
		{
			Advance(TimeSpan.FromSeconds(1));
		}
	}
}