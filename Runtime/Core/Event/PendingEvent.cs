//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.LocalCommands.Core
{
	/// <summary>
	/// Represents an external event queued during command execution, waiting to be
	/// dispatched to subscribers after execution completes.
	/// </summary>
	internal readonly struct PendingEvent
	{
		public readonly Type EventType;
		public readonly object Payload;

		public PendingEvent(Type eventType, object payload)
		{
			EventType = eventType;
			Payload = payload;
		}
	}
}
