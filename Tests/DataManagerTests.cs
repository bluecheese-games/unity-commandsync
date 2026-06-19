//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.CommandSync.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BlueCheese.CommandSync.Tests
{
	// Verifies that ImportState rejects unknown type names instead of silently discarding data.
	[TestFixture]
	public class ImportStateTests
	{
		private DataManager _dataManager;

		[SetUp]
		public void Setup()
		{
			_dataManager = new DataManager(new FakeDataStorage(), new NewtonsoftJsonSerializer());
		}

		[Test]
		public void ImportState_WithUnknownTypeName_Throws()
		{
			var stateWithUnknownType = new Dictionary<string, string>
			{
				[typeof(TestScoreData).FullName] = "{\"Score\":42}",
				["BlueCheese.CommandSync.Tests.ObsoletePlayerData"] = "{\"Level\":5}",
			};

			Assert.Throws<InvalidOperationException>(() => _dataManager.ImportState(stateWithUnknownType),
				"ImportState must throw when a type cannot be resolved instead of silently discarding the data.");
		}

		[Test]
		public void ImportState_WithAllKnownTypes_ImportsCorrectly()
		{
			var validState = new Dictionary<string, string>
			{
				[typeof(TestScoreData).FullName] = "{\"Score\":99}",
			};

			Assert.DoesNotThrow(() => _dataManager.ImportState(validState));
			Assert.AreEqual(99, _dataManager.Get<TestScoreData>().Score);
		}
	}

	// Verifies that mutating a DataBox directly without going through Data.Update<T>
	// does not bypass dirty-tracking and does not silently persist changes.
	// After the fix, GetBox<T> is no longer on the public IDataManager interface,
	// making this bypass impossible for command authors.
	[TestFixture]
	public class DataBoxProtectionTests
	{
		[Test]
		public void GetBox_MutatingValueDirectly_WithoutSettingDirty_IsNotPersisted()
		{
			var storage = new FakeDataStorage();
			var dataManager = new DataManager(storage, new NewtonsoftJsonSerializer());

			var box = dataManager.GetBox<TestScoreData>();
			box.Value = new TestScoreData { Score = 999 };
			// IsDirty is NOT set — the change must not be flushed

			dataManager.Flush();

			bool persisted = storage.TryLoad<TestScoreData>(typeof(TestScoreData).FullName, out var loaded);

			Assert.IsFalse(persisted && loaded.Score == 999,
				"A DataBox mutation without IsDirty = true must not be flushed to storage.");
		}
	}

	// Verifies that user data and command history coexist in the same storage
	// without key collisions when a separate storage is not provided.
	[TestFixture]
	public class StorageIsolationTests
	{
		[Test]
		public void SameStorage_UserDataAndHistoryDoNotCollide()
		{
			var sharedStorage = new FakeDataStorage();
			var dataManager = new DataManager(sharedStorage, new NewtonsoftJsonSerializer());
			var manager = new CommandManager(
				dataManager, new FakeLogger(), Config.Create(),
				new SystemTimeProvider(), sharedStorage);
			manager.RegisterCommands(Assembly.GetExecutingAssembly());

			manager.ExecuteCommand(nameof(TestCommands.AddScore), new TestArgs { Value = 7 });

			bool historyPresent = sharedStorage.TryLoad(
				"CommandSync_History", typeof(CommandHistory), out var historyObj);
			bool dataPresent = sharedStorage.TryLoad<TestScoreData>(
				typeof(TestScoreData).FullName, out var scoreData);

			Assert.IsTrue(historyPresent, "History must be present in storage.");
			Assert.IsTrue(dataPresent, "User data must also be present in storage.");
			Assert.AreEqual(7, scoreData.Score, "User data must not be corrupted by the history entry.");
			Assert.IsNotNull(historyObj, "History object must not be null.");
		}
	}
}
