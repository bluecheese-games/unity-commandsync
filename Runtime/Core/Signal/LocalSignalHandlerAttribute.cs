//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.LocalCommands.Core
{
	/// <summary>
	/// Marks a static method as a local signal handler.
	/// Signal handlers are executed synchronously and immediately when a signal is sent,
	/// in the middle of the sending command's execution.
	/// Rules:
	/// - Static method
	/// - First parameter: CommandContext
	/// - Second parameter: the signal payload struct
	/// Signal handlers are not stored in the command history — they are re-executed
	/// implicitly when the triggering command is replayed.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public class LocalSignalHandlerAttribute : Attribute
	{
		/// <summary>
		/// Execution order relative to other handlers for the same signal type.
		/// Higher values execute first. Defaults to 0.
		/// </summary>
		public int Priority { get; }

		public LocalSignalHandlerAttribute(int priority = 0)
		{
			Priority = priority;
		}
	}
}
