//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlueCheese.LocalCommands.Core
{
	public interface ISyncService
	{
		/// <summary>
		/// Sends a batch of commands to the server and receives the execution result.
		/// </summary>
		Task<SyncResponse> SyncAsync(SyncRequest request);

		/// <summary>
		/// Fetches the full authoritative state from the server.
		/// Returns a dictionary of serialized data keyed by their TypeFullName.
		/// </summary>
		Task<FetchResponse> FetchAsync();
	}
}
