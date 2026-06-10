//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.LocalCommands.Core
{
	public interface IEventContext
	{
		/// <summary>
		/// Raises a local event from within a command or an event handler.
		/// The event will be dispatched to all registered handlers after the current
		/// command finishes executing, before the data is flushed to storage.
		/// </summary>
		void Raise<T>(T eventData) where T : struct;
	}
}
