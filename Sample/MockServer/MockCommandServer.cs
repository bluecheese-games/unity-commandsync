using System;
using System.Collections.Generic;
using System.Linq;
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
		// Reserved storage key for the persisted list of known data types (see _knownTypes below).
		// Not a valid C# Type.FullName, so it cannot collide with a real data type's storage entry.
		private const string KnownTypesStorageKey = "__MockCommandServer_KnownTypes__";

		private readonly ISerializer _serializer;
		private readonly IDataStorage _storage;
		private readonly DataManager _dataManager;
		private readonly CommandManager _manager;
		private readonly HashSet<Type> _knownTypes = new(); // Every data type the server has ever touched, for full-state export.

		/// <summary>
		/// When true, the server double-applies each batch to fabricate a genuine state divergence —
		/// useful to demo and test the desync recovery path. The hash mismatch is real, not faked.
		/// </summary>
		public bool ForceDesync { get; set; }

		/// <param name="dataStorage">
		/// Backing storage for the server's authoritative state. Defaults to an in-memory store (fresh,
		/// empty state every time a MockCommandServer is constructed — the right choice for isolated unit
		/// tests). Pass a persistent storage (e.g. a PlayerPrefs-backed one, with its own prefix, distinct
		/// from the client's) to survive across Play Mode restarts like a real backend's database would —
		/// otherwise the "server" forgets everything on every restart while a client using persistent local
		/// storage does not, which manifests as a spurious desync as soon as the client acts on data the
		/// restarted server never saw.
		/// </param>
		public MockCommandServer(ISerializer serializer = null, IDataStorage dataStorage = null)
		{
			_serializer = serializer ?? new NewtonsoftJsonSerializer();
			_storage = dataStorage ?? new InMemoryDataStorage();
			_dataManager = new DataManager(_storage, _serializer);
			_manager = new CommandManager(_dataManager, serializer: _serializer);

			// _knownTypes itself is in-memory bookkeeping, so restore it from a previous session too —
			// otherwise HandleFetch would under-report even when the underlying storage is persistent.
			if (_storage.TryLoad(KnownTypesStorageKey, out string[] knownTypeNames))
			{
				foreach (var typeName in knownTypeNames)
				{
					var type = TypeResolver.Resolve(typeName);
					if (type != null)
					{
						_knownTypes.Add(type);
					}
				}
			}
		}

		/// <summary>Registers the command definitions the server will replay (must match the client's).</summary>
		public void RegisterCommands(Assembly assembly) => _manager.RegisterCommands(assembly);

		/// <summary>
		/// Installs a plugin on the server, mirroring <see cref="CommandManager.AddPlugin"/> on the client.
		/// Without this, adding a plugin to the client alone (e.g. <c>clientManager.AddPlugin(new
		/// LeaderboardPlugin())</c>) silently desyncs on the very first command the plugin defines, since the
		/// server never learns about it — the caller had to remember a separate
		/// <c>RegisterCommands(pluginAssembly)</c> call to keep both sides in lock-step.
		/// </summary>
		public void AddPlugin(IPlugin plugin) => _manager.AddPlugin(plugin);

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

			RememberKnownTypes(_manager.UpdatedDataTypes);

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

		private void RememberKnownTypes(IEnumerable<Type> types)
		{
			bool changed = false;
			foreach (var type in types)
			{
				if (_knownTypes.Add(type))
				{
					changed = true;
				}
			}

			if (changed)
			{
				_storage.Save(KnownTypesStorageKey, _knownTypes.Select(t => t.FullName).ToArray());
			}
		}
	}
}
