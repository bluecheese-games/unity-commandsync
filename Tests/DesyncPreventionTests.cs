using BlueCheese.CommandSync.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlueCheese.CommandSync.Tests
{
	// Verifies that GetStateHash is deterministic regardless of type enumeration order.
	[TestFixture]
	public class HashDeterminismTests
	{
		private DataManager _dataManager;

		[SetUp]
		public void Setup()
		{
			_dataManager = new DataManager(new FakeDataStorage(), new NewtonsoftJsonSerializer());
		}

		[Test]
		public void GetStateHash_WithNullArg_IsDeterministicAcrossMultipleCalls()
		{
			_dataManager.Set(new TestScoreData { Score = 42 });
			_dataManager.Set(new ValueTypeArgs { Id = 7 });
			_dataManager.Flush();

			long hash1 = _dataManager.GetStateHash(null);
			long hash2 = _dataManager.GetStateHash(null);

			Assert.AreEqual(hash1, hash2,
				"GetStateHash(null) must return the same value on consecutive calls for the same state.");
		}

		[Test]
		public void GetStateHash_WithExplicitTypes_IsDeterministicRegardlessOfOrder()
		{
			_dataManager.Set(new TestScoreData { Score = 10 });
			_dataManager.Set(new ValueTypeArgs { Id = 5 });
			_dataManager.Flush();

			var order1 = new List<Type> { typeof(TestScoreData), typeof(ValueTypeArgs) };
			var order2 = new List<Type> { typeof(ValueTypeArgs), typeof(TestScoreData) };

			Assert.AreEqual(
				_dataManager.GetStateHash(order1),
				_dataManager.GetStateHash(order2),
				"GetStateHash must produce the same hash regardless of the order of the input types.");
		}

		[Test]
		public void GetStateHash_WithEmptyList_ReturnsSeedOnly()
		{
			_dataManager.Set(new TestScoreData { Score = 100 });
			_dataManager.Flush();

			Assert.AreEqual(17, _dataManager.GetStateHash(new List<Type>()),
				"An empty type list should produce the initial seed value (17) — no data is included.");
		}
	}

	[TestFixture]
	public class DesyncPreventionTests
	{
		private CommandManager _manager;
		private DataManager _dataManager;

		[SetUp]
		public void Setup()
		{
			var storage = new FakeDataStorage();
			var serializer = new NewtonsoftJsonSerializer();
			_dataManager = new DataManager(storage, serializer);

			var config = Config.Create();
			_manager = new CommandManager(
				_dataManager,
				new FakeLogger(),
				config,
				new SystemTimeProvider(),
				storage,
				new FakeCommandSyncService()
			);

			_manager.RegisterCommands(Assembly.GetExecutingAssembly());
		}

		[Test]
		public void ClearHistory_ProperlyClearsUpdatedDataTypes()
		{
			// Act: Execute a command that updates TestScoreData
			_manager.ExecuteCommand(nameof(TestCommands.AddScore), new TestArgs { Value = 10 });

			// Assert before clear
			Assert.AreEqual(1, _manager.History.Count, "Command should be queued.");
			Assert.IsTrue(_manager.UpdatedDataTypes.Contains(typeof(TestScoreData)), "UpdatedDataTypes should track the modification.");

			// Act: Clear the history (simulating successful sync or server request reset)
			_manager.ClearHistory();

			// Assert after clear
			Assert.AreEqual(0, _manager.History.Count, "Queue should be empty.");
			Assert.AreEqual(0, _manager.UpdatedDataTypes.Count(), "UpdatedDataTypes must be completely cleared to prevent desync accumulation.");
		}

		[Test]
		public void GetStateHash_WithEmptyCollection_ComputesEmptyDeltaInsteadOfFallback()
		{
			// Arrange: Populate cache with some data
			_dataManager.Set(new TestScoreData { Score = 100 });
			_dataManager.Flush();

			// Act: Hash using an explicitly empty list (simulating a sync request where no data changed)
			long hashEmpty = _dataManager.GetStateHash(new List<Type>());

			// Act: Hash using null (simulating a full state hash fallback)
			long hashNull = _dataManager.GetStateHash(null);

			// Assert
			Assert.AreEqual(17, hashEmpty, "An empty list should result in the default hash seed (17), not fallback to the entire cache.");
			Assert.AreNotEqual(17, hashNull, "A null argument should fallback to hashing the entire cache.");
		}

		[Test]
		public void GetStateHash_IsDeterministic_RegardlessOfCollectionOrder()
		{
			// Arrange: Populate cache with two different data types
			_dataManager.Set(new TestScoreData { Score = 10 });
			_dataManager.Set(new ValueTypeArgs { Id = 5 });
			_dataManager.Flush();

			// Act: Pass the same types to the hash function, but in different order (like Dictionary.Keys might do randomly)
			var order1 = new List<Type> { typeof(TestScoreData), typeof(ValueTypeArgs) };
			var order2 = new List<Type> { typeof(ValueTypeArgs), typeof(TestScoreData) };

			long hash1 = _dataManager.GetStateHash(order1);
			long hash2 = _dataManager.GetStateHash(order2);

			// Assert
			Assert.AreEqual(hash1, hash2, "The generated hash must be identical regardless of the input IEnumerable order, thanks to internal sorting.");
		}

		[Test]
		public void ServerSyncIsolation_ClearingHistory_ResetsDeltaForNextRequest()
		{
			// This test simulates the server's SyncController behavior.

			// 1. Simulate Request A: Modifies Score
			_manager.ExecuteCommand(nameof(TestCommands.AddScore), new TestArgs { Value = 5 });
			Assert.IsTrue(_manager.UpdatedDataTypes.Contains(typeof(TestScoreData)));

			// 2. Simulate start of Request B: Server calls ClearHistory() to isolate the request
			_manager.ClearHistory();

			// 3. Request B executes a command that modifies NOTHING
			_manager.ExecuteCommand(nameof(TestCommands.NoOpCommand));

			// 4. Server computes the state hash for Request B's delta
			long hash = _dataManager.GetStateHash(_manager.UpdatedDataTypes);

			// Assert
			Assert.AreEqual(17, hash, "Hash should be the default seed (17) because NoOpCommand changed nothing, and the history was properly cleared from Request A's modifications.");
		}
	}
}
