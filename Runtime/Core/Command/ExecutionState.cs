//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.LocalCommands.Core
{
	public enum CommandExecutionResult
	{
		Success,
		Failure,
	}

	public class ExecutionState
	{
		public CommandExecutionResult Result { get; private set; } = CommandExecutionResult.Success;
		public string FailureMessage { get; private set; } = null;
		public bool DataHasBeenUpdated { get; internal set; }

		public void Fail(string message)
		{
			Result = CommandExecutionResult.Failure;
			FailureMessage = message;
		}
	}
}
