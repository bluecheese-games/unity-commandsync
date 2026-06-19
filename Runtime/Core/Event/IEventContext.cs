//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.LocalCommands.Core
{
	public interface IEventContext
	{
		/// <summary>
		/// Queues an event to be dispatched to external subscribers after the current
		/// command (and all its signal handlers) have finished executing.
		/// Use this to communicate outward from the LocalCommands module without
		/// coupling commands to external systems.
		/// </summary>
		void Raise<T>(T eventData) where T : struct;
	}
}
