//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.LocalCommands.Core
{
	/// <summary>
	/// Marks a static method as a local event handler.
	/// The method must follow the same rules as commands:
	/// - Static
	/// - First parameter: Context
	/// - Second parameter: the event payload (struct)
	/// Unlike commands, handlers are not stored in the command history — they are
	/// re-executed implicitly when the triggering command is replayed.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public class LocalEventHandlerAttribute : Attribute { }
}
