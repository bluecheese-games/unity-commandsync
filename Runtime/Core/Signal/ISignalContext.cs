//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.CommandSync.Core
{
	public interface ISignalContext
	{
		/// <summary>
		/// Sends a signal synchronously. All registered handlers for this signal type
		/// are executed immediately before this call returns, in the middle of the
		/// sending command's execution.
		/// </summary>
		void Send<T>(T signal) where T : struct;
	}
}
