using System;
using System.Collections.Generic;
using System.Reflection;
using BlueCheese.CommandSync.Core;

namespace BlueCheese.CommandSync.Sample.MockServer
{
	/// <summary>
	/// In-process stand-in for the authoritative backend. It runs the SAME Core pipeline as the client:
	/// it replays the received commands on its own authoritative state, then compares the resulting state
	/// hash with the client's — returning Ok when they match and Desync otherwise.
	/// Requests and responses are exchanged as serialized JSON to faithfully reproduce the network boundary
	/// (so this also exercises serialization, argument reconstruction and type resolution, not just replay).
	/// Depends only on the Core assembly, so it can run outside of Unity, like a real server.
	/// </summary>
	public sealed class MockCommandServer
	{
		private readonly ISerializer _serializer;
		private readonly DataManager _dataManager;
		private readonly CommandManager _manager;
		private readonly HashSet<Type> _knownTypes = new(); // Every data type the server has ever touched, for full-state export.

		/// <summary>
		/// When true, the server double-applies each batch to fabricate a genuine state divergence —
		/// useful to demo and test the desync recovery path. The hash mismatch is real, not faked.
		/// </summary>
		public bool ForceDesync { get; set; }

		public MockCommandServer(ISerializer serializer = null)
		{
			_serializer = serializer ?? new NewtonsoftJsonSerializer();
			_dataManager = new DataManager(new InMemoryDataStorage(), _serializer);
			_manager = new CommandManager(_dataManager, serializer: _serializer);
		}

		/// <summary>Registers the command definitions the server will replay (must match the client's).</summary>
		public void RegisterCommands(Assembly assembly) => _manager.RegisterCommands(assembly);

		/// <summary>Reads the server's authoritative value for a data type (for inspection in tests).</summary>
		public T GetState<T>() where T : struct => _dataManager.Get<T>();

		/// <summary>Handles a sync request (serialized SyncRequest in, serialized SyncResponse out).</summary>
		public string HandleSync(string requestJson)
		{
			var request = _serializer.Deserialize<SyncRequest>(requestJson);

			// Isolate this request's delta, mirroring a stateless server handler.
			_manager.ClearHistory();

			int passes = ForceDesync ? 2 : 1; // double-apply to diverge from the client's single application
			for (int pass = 0; pass < passes; pass++)
			{
				foreach (var call in request.Commands)
				{
					_manager.ReplayCommand(call);
				}
			}

			foreach (var type in _manager.UpdatedDataTypes)
			{
				_knownTypes.Add(type);
			}

			long serverHash = _dataManager.GetStateHash(_manager.UpdatedDataTypes);

			SyncResponse response = serverHash == request.ClientStateHash
				? SyncResponse.Ok()
				: SyncResponse.Desync($"State hash mismatch (client={request.ClientStateHash}, server={serverHash}).");

			return _serializer.Serialize(response);
		}

		/// <summary>Handles a full-state fetch (serialized FetchResponse out).</summary>
		public string HandleFetch()
		{
			var state = _dataManager.ExportState(_knownTypes);
			return _serializer.Serialize(FetchResponse.Ok(state));
		}
	}
}
