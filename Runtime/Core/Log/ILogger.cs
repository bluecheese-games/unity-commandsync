//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.CommandSync.Core
{
	public interface ILogger
	{
		void Log(string message);

		void LogWarning(string message);

		void LogError(string message);

		void LogException(Exception exception);
	}
}
