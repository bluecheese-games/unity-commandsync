//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.LocalCommands.Core
{
	/// <summary>
	/// Represents an event raised during a command execution, waiting to be dispatched.
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
