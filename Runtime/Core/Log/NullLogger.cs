//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.LocalCommands.Core
{
	public class NullLogger : ILogger
	{
		public void Log(string message)
		{
			// Do nothing
		}

		public void LogWarning(string message)
		{
			// Do nothing
		}

		public void LogError(string message)
		{
			// Do nothing
		}

		public void LogException(Exception exception)
		{
			// Do nothing
		}
	}
}
