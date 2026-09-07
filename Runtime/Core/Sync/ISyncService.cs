using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BlueCheese.CommandSync.Core
{
	public interface ISyncService
	{
		/// <summary>
		/// Sends a batch of commands to the server and receives the execution result.
		/// </summary>
		Task<SyncResponse> SyncAsync(SyncRequest request, CancellationToken cancellationToken = default);

		/// <summary>
		/// Fetches the full authoritative state from the server.
		/// Returns a dictionary of serialized data keyed by their TypeFullName.
		/// </summary>
		Task<FetchResponse> FetchAsync(CancellationToken cancellationToken = default);
	}
}
