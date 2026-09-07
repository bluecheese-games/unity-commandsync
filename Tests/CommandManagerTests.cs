using NUnit.Framework;
using System.Reflection;
using System.Collections.Generic;
using BlueCheese.CommandSync.Core;

namespace BlueCheese.CommandSync.Tests
{
	[TestFixture]
	public class CommandManagerTests
	{
		private CommandManager _manager;
		private ISerializer _serializer;
		private DataManager _dataManager;
		private FakeDataStorage _storage;
		private FakeLogger _logger;
		private FakeCommandSyncService _syncService;

		[SetUp]
		public void Setup()
		{
			_storage = new FakeDataStorage();
			_serializer = new NewtonsoftJsonSerializer();
			_dataManager = new DataManager(_storage, _serializer);
			_logger = new FakeLogger();
			_syncService = new FakeCommandSyncService();

			var config = Config.Create(new Dictionary<string, object> { { "multiplier", 2 } });
			_manager = new CommandManager(_dataManager, _logger, config, new SystemTimeProvider(), _storage, _syncService);
			_manager.RegisterCommands(Assembly.GetExecutingAssembly());
		}

		[Test]
		public void Execute_ByMethodName_Success()
		{
			_manager.ExecuteCommand(nameof(TestCommands.AddScore), new TestArgs { Value = 10 });
			Assert.AreEqual(10, _dataManager.Get<TestScoreData>().Score);
		}

		[Test]
		public void Execute_ByCustomAttributeName_Success()
		{
			// Verifies [Command("SecretName")] correctly resolves
			_manager.ExecuteCommand("SecretName");
			Assert.AreEqual(99, _dataManager.Get<TestScoreData>().Score);
		}

		[Test]
		public void Execute_NonExistentCommand_ThrowsCommandNotFoundException()
		{
			Assert.Throws<CommandNotFoundException>(() =>
			{
				_manager.ExecuteCommand("UnknownID");
			});
		}

		[Test]
		public void Execute_WhenCommandThrows_LogsWarningAndDoesNotEnqueue()
		{
			_manager.ExecuteCommand(nameof(TestCommands.ThrowingCommand));

			Assert.AreEqual(0, _manager.History.Count);
			Assert.IsTrue(_logger.Logs.Exists(l => l.Contains("Intentional crash for testing")));
		}

		[Test]
		public void ClearHistory_SyncsWithDataManagerAndStorage()
		{
			_manager.ExecuteCommand(nameof(TestCommands.AddScore), new TestArgs { Value = 1 });
			_ = _manager.Sync();

			Assert.AreEqual(0, _manager.History.Count);
			bool loaded = _storage.TryLoad("CommandSync_History", typeof(CommandHistory), out var historyObj);
			Assert.IsTrue(loaded);
			Assert.AreEqual(0, ((CommandHistory)historyObj).ToArray().Length);
		}
	}
}
