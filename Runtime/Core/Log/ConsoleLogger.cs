//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.LocalCommands.Core
{
	public class ConsoleLogger : ILogger
	{
		public void Log(string message)
		{
			Console.WriteLine(message);
		}

		public void LogWarning(string message)
		{
			Console.WriteLine($"WARNING: {message}");
		}

		public void LogError(string message)
		{
			Console.WriteLine($"ERROR: {message}");
		}

		public void LogException(Exception exception)
		{
			Console.WriteLine($"EXCEPTION: {exception}");
		}
	}
}
