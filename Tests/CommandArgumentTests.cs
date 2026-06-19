//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.CommandSync.Core;
using NUnit.Framework;
using System.Reflection;

namespace BlueCheese.CommandSync.Tests
{
	[TestFixture]
	public class CommandArgumentTests
	{
		private CommandManager _manager;
		private DataManager _dataManager;

		[SetUp]
		public void Setup()
		{
			var storage = new FakeDataStorage();
			_dataManager = new DataManager(storage, null);

			// Initializing with public factory method
			var config = Config.Create();

			_manager = new CommandManager(
				_dataManager,
				new FakeLogger(),
				config,
				new SystemTimeProvider(),
				storage
			);

			_manager.RegisterCommands(Assembly.GetExecutingAssembly());
		}

		[Test]
		public void Execute_MissingRequiredArgs_ThrowsCommandArgumentException()
		{
			// AddScore expects TestArgs (class), but we pass null 
			Assert.Throws<CommandArgumentException>(() =>
			{
				_manager.ExecuteCommand(nameof(TestCommands.AddScore), null);
			});
		}

		[Test]
		public void Execute_ProvidingArgsToNoArgCommand_ThrowsCommandArgumentException()
		{
			// NoOpCommand accepts no args, but we provide an object 
			Assert.Throws<CommandArgumentException>(() =>
			{
				_manager.ExecuteCommand(nameof(TestCommands.NoOpCommand), new TestArgs());
			});
		}

		[Test]
		public void Execute_WrongArgumentType_ThrowsCommandArgumentException()
		{
			// AddScore expects TestArgs, but we pass a string 
			Assert.Throws<CommandArgumentException>(() =>
			{
				_manager.ExecuteCommand(nameof(TestCommands.AddScore), "Invalid String Argument");
			});
		}

		[Test]
		public void Execute_PassNullToValueTypeArg_ThrowsCommandArgumentException()
		{
			// ValueTypeCommand expects a struct, but null is passed 
			Assert.Throws<CommandArgumentException>(() =>
			{
				_manager.ExecuteCommand(nameof(TestCommands.ValueTypeCommand), null);
			});
		}
	}
}