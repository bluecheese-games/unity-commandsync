using System.Threading;
using System.Threading.Tasks;
using BlueCheese.CommandSync.Core;
using UnityEngine;

/// <summary>Bare-minimum ISyncService that always succeeds and returns empty state — a starting point for
/// wiring your own transport, kept here for reference rather than used by the sample controller itself
/// (which talks to the in-process MockCommandServer instead, see Sample/MockServer).</summary>
public class DummyCommandSyncService : ISyncService
{
	public Task<SyncResponse> SyncAsync(SyncRequest request, CancellationToken cancellationToken = default)
	{
		Debug.Log($"Syncing {request.Commands.Length} commands to server...");
		foreach (var cmd in request.Commands)
		{
			Debug.Log($" - {cmd}");
		}

		// Simulate a successful sync with no conflicts
		return Task.FromResult(SyncResponse.Ok());
	}

	public Task<FetchResponse> FetchAsync(CancellationToken cancellationToken = default)
	{
		// Simulate fetching full state from server (empty in this dummy implementation)
		return Task.FromResult(FetchResponse.Ok(new()));
	}
}
