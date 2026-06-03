//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.LocalCommands.Core;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace BlueCheese.LocalCommands.Tests
{
	[TestFixture]
	public class SyncBehaviorTests
	{
		private LocalCommandManager _manager;
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
			var successSyncService = new FakeCommandSyncService(); // Returns SyncResult.Success by default
			_manager = new LocalCommandManager(_dataManager, new FakeLogger(), _config, new TimeProvider(), _storage, successSyncService);
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
			_manager = new LocalCommandManager(_dataManager, new FakeLogger(), _config, new TimeProvider(), _storage, failingSyncService);
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
			public Task<SyncResponse> SyncAsync(SyncRequest request)
			{
				return Task.FromResult(SyncResponse.Fail("Desynchronized"));
			}

			public Task<FetchResponse> FetchAsync()
			{
				return Task.FromResult(FetchResponse.Fail("Fetch failed"));
			}
		}
	}
}