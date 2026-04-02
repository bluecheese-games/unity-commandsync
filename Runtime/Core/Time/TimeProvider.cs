//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.LocalCommands.Core
{
	public class TimeProvider : ITimeProvider
	{
		public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
	}
}
