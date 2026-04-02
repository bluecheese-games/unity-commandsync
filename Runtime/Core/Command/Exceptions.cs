//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.LocalCommands.Core
{
	public class LocalCommandException : Exception
	{
		public LocalCommandException(string message) : base(message) { }
	}

	public class CommandNotFoundException : LocalCommandException
	{
		public CommandNotFoundException(string commandName)
			: base($"No command registered with the name '{commandName}'.") { }
	}

	public class CommandRegistrationException : LocalCommandException
	{
		public CommandRegistrationException(string message) : base(message) { }
	}

	public class CommandArgumentException : LocalCommandException
	{
		public CommandArgumentException(string message) : base(message) { }
	}
}