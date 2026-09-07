using BlueCheese.CommandSync.Core;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace BlueCheese.CommandSync.Tests
{
	// Verifies that a failed command leaves no partial state behind (transactional rollback)
	// and that reads observe writes made earlier in the same command (read-your-writes).
	[TestFixture]
	public class ExecutionBehaviorTests
	{
		private CommandManager _manager;
		private DataManager _dataManager;

		[SetUp]
		public void Setup()
		{
			var storage = new FakeDataStorage();
			_dataManager = new DataManager(storage, new NewtonsoftJsonSerializer());
			_manager = new CommandManager(
				_dataManager, new FakeLogger(), Config.Create(),
				new SystemTimeProvider(), storage);
			_manager.RegisterCommands(Assembly.GetExecutingAssembly());
		}

		[Test]
		public void FailedCommand_DoesNotPersistItsMutations()
		{
			// Arrange & Act
			_manager.ExecuteCommand(nameof(TestCommands.MutateThenFail));

			// Assert
			Assert.AreEqual(0, _dataManager.Get<TestScoreData>().Score,
				"A failed command must roll back the data it mutated before failing.");
			Assert.AreEqual(0, _manager.History.Count,
				"A failed command must not be enqueued in the history.");
		}

		[Test]
		public void FailedCommand_DoesNotLeakMutationsIntoNextCommand()
		{
			// Arrange
			_manager.ExecuteCommand(nameof(TestCommands.MutateThenFail));

			// Act
			_manager.ExecuteCommand(nameof(TestCommands.AddScore), new TestArgs { Value = 7 });

			// Assert
			Assert.AreEqual(7, _dataManager.Get<TestScoreData>().Score,
				"The next successful command must start from the committed state, not the rolled-back one.");
		}

		[Test]
		public void Get_WithinSameCommand_ObservesEarlierWrite()
		{
			// Arrange & Act
			_manager.ExecuteCommand(nameof(TestCommands.ReadAfterWrite));

			// Assert
			Assert.AreEqual(5, _dataManager.Get<ReadbackData>().Value,
				"Data.Get must return the value written earlier in the same command (read-your-writes).");
		}

		[Test]
		public void ReplayCommand_WithArgsAsJObject_DeserializesThroughSerializer()
		{
			// Arrange: simulate args reloaded from persisted history, where they come back as a JObject
			// instead of the original struct. The manager must reinterpret them via ISerializer, not Newtonsoft directly.
			var jObjectArgs = JObject.FromObject(new TestArgs { Value = 10 });
			var call = new CommandCall
			{
				Id = Guid.NewGuid(),
				CommandName = nameof(TestCommands.AddScore),
				Args = jObjectArgs,
			};

			// Act
			_manager.ReplayCommand(call);

			// Assert
			Assert.AreEqual(10, _dataManager.Get<TestScoreData>().Score,
				"ReplayCommand must convert JObject args to the expected type and apply the command.");
		}
	}
}
