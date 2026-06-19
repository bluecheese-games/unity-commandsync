//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.CommandSync.Core
{
	/// <summary>
	/// Dispatches signals synchronously by invoking the provided dispatch callback immediately.
	/// The callback is a closure created by CommandManager that routes the signal
	/// to the appropriate registered handlers.
	/// </summary>
	internal class SignalContext : ISignalContext
	{
		/// <summary>
		/// Maximum number of nested signal handler levels within a single command execution.
		/// Prevents stack overflows when handlers send signals that trigger other handlers.
		/// </summary>
		public const int MaxCascadeDepth = 8;

		private readonly Action<Type, object> _dispatch;

		internal SignalContext(Action<Type, object> dispatch)
		{
			_dispatch = dispatch;
		}

		public void Send<T>(T signal) where T : struct
		{
			_dispatch(typeof(T), signal);
		}
	}
}
