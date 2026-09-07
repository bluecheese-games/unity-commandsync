using System;

namespace BlueCheese.CommandSync.Core
{
	public interface ITimeProvider
	{
		DateTimeOffset UtcNow { get; }
	}
}
