using System;

namespace BlueCheese.CommandSync.Core
{
	public class SystemTimeProvider : ITimeProvider
	{
		public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
	}
}
