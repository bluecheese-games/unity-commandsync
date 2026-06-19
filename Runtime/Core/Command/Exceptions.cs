//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.CommandSync.Core
{
	public class CommandException : Exception
	{
		public CommandException(string message) : base(message) { }
	}

	public class CommandNotFoundException : CommandException
	{
		public CommandNotFoundException(string commandName)
			: base($"No command registered with the name '{commandName}'.") { }
	}

	public class CommandRegistrationException : CommandException
	{
		public CommandRegistrationException(string message) : base(message) { }
	}

	public class CommandArgumentException : CommandException
	{
		public CommandArgumentException(string message) : base(message) { }
	}
}