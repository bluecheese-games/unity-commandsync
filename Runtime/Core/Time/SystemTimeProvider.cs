//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.CommandSync.Core
{
	public class SystemTimeProvider : ITimeProvider
	{
		public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
	}
}
