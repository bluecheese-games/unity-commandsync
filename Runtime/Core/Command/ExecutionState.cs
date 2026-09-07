using System;

namespace BlueCheese.CommandSync.Core
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
		public Exception FailureException { get; private set; } = null;
		public bool DataHasBeenUpdated { get; internal set; }

		public void Fail(string message, Exception exception = null)
		{
			Result = CommandExecutionResult.Failure;
			FailureMessage = message;
			FailureException = exception;
		}
	}
}
