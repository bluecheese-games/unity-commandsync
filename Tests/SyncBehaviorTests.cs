using BlueCheese.CommandSync.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace BlueCheese.CommandSync.Tests
{
	[TestFixture]
	public class SyncBehaviorTests
	{
		private CommandManager _manager;
		private DataManager _dataManager;
		private FakeDataStorage _storage;
		private Config _config;

		[SetUp]
		public void Setup()
		{
			_storage = new FakeDataStorage();
			_dataManager = new DataManager(_storage, new NewtonsoftJsonSerializer());
			_config = Config.Create();
		}

		[Test]
		public async Task Sync_WhenSuccessful_ClearsHistory()
		{
			// Arrange: Inject a fake sync service that always returns Success
			var successSyncService = new FakeCommandSyncService(); // Returns a successful SyncResponse by default
			_manager = new CommandManager(_dataManager, new FakeLogger(), _config, new SystemTimeProvider(), _storage, successSyncService);
			_manager.RegisterCommands(Assembly.GetExecutingAssembly());

			// Act: Execute a command to populate history, then sync
			_manager.ExecuteCommand(nameof(TestCommands.AddScore), new TestArgs { Value = 50 });
			Assert.AreEqual(1, _manager.History.Count, "History should contain 1 command before sync.");

			await _manager.Sync();

			// Assert: History should be cleared
			Assert.AreEqual(0, _manager.History.Count, "History must be cleared after a successful sync.");
		}

		[Test]
		public async Task Sync_WhenDesyncOrError_RetainsHistory()
		{
			// Arrange: Inject a custom fake sync service that returns Desync
			var failingSyncService = new FailingCommandSyncService();
			_manager = new CommandManager(_dataManager, new FakeLogger(), _config, new SystemTimeProvider(), _storage, failingSyncService);
			_manager.RegisterCommands(Assembly.GetExecutingAssembly());

			// Act: Execute a command and attempt to sync
			_manager.ExecuteCommand(nameof(TestCommands.AddScore), new TestArgs { Value = 50 });

			await _manager.Sync();

			// Assert: History should remain intact for future retries or rollback
			Assert.AreEqual(1, _manager.History.Count, "History must be retained if sync fails or desyncs.");
		}

		// A specific fake service for simulating failures
		private class FailingCommandSyncService : ISyncService
		{
			public Task<SyncResponse> SyncAsync(SyncRequest request, CancellationToken cancellationToken = default)
			{
				return Task.FromResult(SyncResponse.Fail("Desynchronized"));
			}

			public Task<FetchResponse> FetchAsync(CancellationToken cancellationToken = default)
			{
				return Task.FromResult(FetchResponse.Fail("Fetch failed"));
			}
		}
	}

	// Verifies that concurrent Sync() calls do not send the same commands twice.
	[TestFixture]
	public class SyncConcurrencyTests
	{
		[Test]
		public async Task Sync_CalledConcurrently_DoesNotSendCommandsTwice()
		{
			var storage = new FakeDataStorage();
			var dataManager = new DataManager(storage, new NewtonsoftJsonSerializer());
			var syncService = new CountingSyncService();

			var manager = new CommandManager(
				dataManager, new FakeLogger(), Config.Create(),
				new SystemTimeProvider(), storage, syncService);
			manager.RegisterCommands(Assembly.GetExecutingAssembly());

			manager.ExecuteCommand(nameof(TestCommands.AddScore), new TestArgs { Value = 5 });

			var t1 = manager.Sync();
			var t2 = manager.Sync();
			await Task.WhenAll(t1, t2);

			Assert.AreEqual(1, syncService.SyncCallCount,
				"Concurrent Sync() calls must only send commands once, not twice.");
		}

		private class CountingSyncService : ISyncService
		{
			public int SyncCallCount { get; private set; }

			public Task<SyncResponse> SyncAsync(SyncRequest request, CancellationToken cancellationToken = default)
			{
				SyncCallCount++;
				return Task.FromResult(SyncResponse.Ok());
			}

			public Task<FetchResponse> FetchAsync(CancellationToken cancellationToken = default) =>
				Task.FromResult(FetchResponse.Ok(new Dictionary<string, string>()));
		}
	}

	// Verifies that persisted CommandCall.Args are correctly deserialized after an app restart.
	// When history is reloaded from storage, Args come back as JObject (Newtonsoft) instead of
	// the original typed struct. ReplayCommand must handle this transparently.
	//
	// Setup: the history storage is shared across sessions (it survives the restart),
	// but the data storage is fresh in session 2 — replay is meant to reconstruct state
	// from commands, so the starting point must be empty.
	[TestFixture]
	public class ArgsPersistenceTests
	{
		[Test]
		public void ReplayCommand_AfterPersistence_CorrectlyDeserializesArgs()
		{
			var historyStorage = new FakeDataStorage(); // survives across sessions
			var serializer = new NewtonsoftJsonSerializer();

			// Session 1: execute a command — data and history are written
			var dataManager1 = new DataManager(new FakeDataStorage(), serializer);
			var manager1 = new CommandManager(
				dataManager1, new FakeLogger(), Config.Create(),
				new SystemTimeProvider(), historyStorage);
			manager1.RegisterCommands(Assembly.GetExecutingAssembly());
			manager1.ExecuteCommand(nameof(TestCommands.AddScore), new TestArgs { Value = 10 });

			// Session 2: fresh data storage, same history storage (simulates restart with lost data)
			// Args in the reloaded history may now be JObject instead of TestArgs
			var dataManager2 = new DataManager(new FakeDataStorage(), serializer);
			var manager2 = new CommandManager(
				dataManager2, new FakeLogger(), Config.Create(),
				new SystemTimeProvider(), historyStorage);
			manager2.RegisterCommands(Assembly.GetExecutingAssembly());

			var reloadedCall = manager2.History[0];

			Assert.DoesNotThrow(() => manager2.ReplayCommand(reloadedCall),
				"ReplayCommand must deserialize persisted args correctly regardless of their runtime type.");

			Assert.AreEqual(10, dataManager2.Get<TestScoreData>().Score,
				"The replayed command must produce the correct result on a fresh data state.");
		}
	}

	// Verifies that when the server reports a desync, the client recovers by re-fetching and
	// importing the authoritative state, and clears its local history.
	[TestFixture]
	public class DesyncRecoveryTests
	{
		[Test]
		public async Task Sync_WhenServerReportsDesync_ReimportsAuthoritativeStateAndClearsHistory()
		{
			var serializer = new NewtonsoftJsonSerializer();
			var storage = new FakeDataStorage();
			var dataManager = new DataManager(storage, serializer);

			var serverState = new Dictionary<string, string>
			{
				[typeof(TestScoreData).FullName] = serializer.Serialize(new TestScoreData { Score = 999 }, typeof(TestScoreData)),
			};

			var manager = new CommandManager(
				dataManager, new FakeLogger(), Config.Create(),
				new SystemTimeProvider(), storage, new DesyncThenFetchSyncService(serverState));
			manager.RegisterCommands(Assembly.GetExecutingAssembly());

			manager.ExecuteCommand(nameof(TestCommands.AddScore), new TestArgs { Value = 5 });
			Assert.AreEqual(1, manager.History.Count, "Command should be queued before sync.");

			await manager.Sync();

			Assert.AreEqual(0, manager.History.Count, "Desync recovery must clear the local history.");
			Assert.AreEqual(999, dataManager.Get<TestScoreData>().Score,
				"Client must adopt the authoritative server state after a desync.");
		}

		private class DesyncThenFetchSyncService : ISyncService
		{
			private readonly Dictionary<string, string> _serverState;

			public DesyncThenFetchSyncService(Dictionary<string, string> serverState) => _serverState = serverState;

			public Task<SyncResponse> SyncAsync(SyncRequest request, CancellationToken cancellationToken = default) =>
				Task.FromResult(SyncResponse.Desync("Client state hash mismatch."));

			public Task<FetchResponse> FetchAsync(CancellationToken cancellationToken = default) =>
				Task.FromResult(FetchResponse.Ok(_serverState));
		}
	}
}
