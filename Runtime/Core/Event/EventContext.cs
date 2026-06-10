//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System.Collections.Generic;

namespace BlueCheese.LocalCommands.Core
{
	/// <summary>
	/// Collects events raised during a command or handler execution.
	/// The LocalCommandManager drains this queue after each execution step.
	/// </summary>
	internal class EventContext : IEventContext
	{
		/// <summary>
		/// Maximum number of cascading event handler levels allowed during a single command execution.
		/// Prevents infinite loops when handlers raise events that trigger other handlers.
		/// </summary>
		public const int MaxCascadeDepth = 8;

		private readonly Queue<PendingEvent> _pendingEvents = new();

		public bool HasPendingEvents => _pendingEvents.Count > 0;

		public void Raise<T>(T eventData) where T : struct
		{
			_pendingEvents.Enqueue(new PendingEvent(typeof(T), eventData));
		}

		/// <summary>
		/// Dequeues and returns the next pending event, or returns false if the queue is empty.
		/// </summary>
		public bool TryDequeue(out PendingEvent pendingEvent)
		{
			if (_pendingEvents.Count == 0)
			{
				pendingEvent = default;
				return false;
			}

			pendingEvent = _pendingEvents.Dequeue();
			return true;
		}
	}
}
