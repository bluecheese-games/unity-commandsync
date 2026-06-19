//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.CommandSync.Core
{
	public interface ITimeProvider
	{
		DateTimeOffset UtcNow { get; }
	}
}
