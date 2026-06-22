//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System.Threading;
using System.Threading.Tasks;
using BlueCheese.CommandSync.Core;

namespace BlueCheese.CommandSync.Sample.MockServer
{
	/// <summary>
	/// <see cref="ISyncService"/> that talks to an in-process <see cref="MockServer"/> over serialized JSON,
	/// reproducing the request/response round-trip of a real HTTP transport (minus the actual network).
	/// </summary>
	public sealed class MockServerSyncService : ISyncService
	{
		private readonly MockCommandServer _server;
		private readonly ISerializer _serializer;
		private readonly int _simulatedLatencyMs;

		public MockServerSyncService(MockCommandServer server, ISerializer serializer = null, int simulatedLatencyMs = 0)
		{
			_server = server;
			_serializer = serializer ?? new NewtonsoftJsonSerializer();
			_simulatedLatencyMs = simulatedLatencyMs;
		}

		public async Task<SyncResponse> SyncAsync(SyncRequest request, CancellationToken cancellationToken = default)
		{
			string requestJson = _serializer.Serialize(request);
			await SimulateLatency(cancellationToken);
			string responseJson = _server.HandleSync(requestJson);
			return _serializer.Deserialize<SyncResponse>(responseJson);
		}

		public async Task<FetchResponse> FetchAsync(CancellationToken cancellationToken = default)
		{
			await SimulateLatency(cancellationToken);
			string responseJson = _server.HandleFetch();
			return _serializer.Deserialize<FetchResponse>(responseJson);
		}

		private Task SimulateLatency(CancellationToken cancellationToken) =>
			_simulatedLatencyMs > 0 ? Task.Delay(_simulatedLatencyMs, cancellationToken) : Task.CompletedTask;
	}
}
