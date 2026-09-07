using System;
using System.Reflection;
using BlueCheese.CommandSync.Core;
using NUnit.Framework;

namespace BlueCheese.CommandSync.Tests
{
	public interface IGreetingService
	{
		string Greet(string name);
	}

	public class FakeGreetingService : IGreetingService
	{
		public string Greet(string name) => $"Hello, {name}!";
	}

	public struct GreetingData
	{
		public string LastGreeting;
	}

	public struct GreetArgs { public string Name; }

	public struct GreetingRequestedSignal { public string Name; }

	public sealed class GreetingPlugin : IPlugin
	{
		private readonly IGreetingService _service;

		public GreetingPlugin(IGreetingService service) => _service = service;

		public string Name => "Greeting";

		public void Install(PluginInstallContext context)
		{
			context.Services.Add(_service);
		}
	}

	// Demonstrates the intended usage: a plugin wraps CommandContext.GetService behind a named
	// extension method so the service shows up in auto-completion, e.g. `ctx.Greeting()`.
	public static class GreetingContextExtensions
	{
		public static IGreetingService Greeting(this CommandContext ctx) => ctx.GetService<IGreetingService>();
	}

	public static class GreetingCommands
	{
		[Command]
		public static void Greet(CommandContext context, GreetArgs args)
		{
			var message = context.Greeting().Greet(args.Name);
			context.Data.Update((ref GreetingData data) => data.LastGreeting = message);
		}

		[Command]
		public static void RequestGreetingViaSignal(CommandContext context, GreetArgs args)
		{
			context.Signals.Send(new GreetingRequestedSignal { Name = args.Name });
		}

		[SignalHandler]
		public static void OnGreetingRequested(CommandContext context, GreetingRequestedSignal signal)
		{
			var message = context.Greeting().Greet(signal.Name);
			context.Data.Update((ref GreetingData data) => data.LastGreeting = message);
		}
	}

	[TestFixture]
	public class PluginTests
	{
		private CommandManager _manager;
		private DataManager _dataManager;
		private FakeLogger _logger;

		[SetUp]
		public void Setup()
		{
			var storage = new FakeDataStorage();
			_dataManager = new DataManager(storage, new NewtonsoftJsonSerializer());
			_logger = new FakeLogger();
			_manager = new CommandManager(_dataManager, _logger, Config.Create(), new SystemTimeProvider(), storage);
		}

		[Test]
		public void AddPlugin_RegistersItsCommands_WithoutManualRegisterCommandsCall()
		{
			// Only AddPlugin is called, no RegisterCommands(Assembly) — proves the plugin's own
			// assembly is scanned automatically, which is what makes plugins work from a separate package.
			_manager.AddPlugin(new GreetingPlugin(new FakeGreetingService()));

			_manager.ExecuteCommand(nameof(GreetingCommands.Greet), new GreetArgs { Name = "Ada" });

			Assert.AreEqual("Hello, Ada!", _dataManager.Get<GreetingData>().LastGreeting);
		}

		[Test]
		public void SignalHandler_CanAlsoAccessPluginServices()
		{
			_manager.AddPlugin(new GreetingPlugin(new FakeGreetingService()));

			_manager.ExecuteCommand(nameof(GreetingCommands.RequestGreetingViaSignal), new GreetArgs { Name = "Alan" });

			Assert.AreEqual("Hello, Alan!", _dataManager.Get<GreetingData>().LastGreeting);
		}

		[Test]
		public void AddPlugin_DuplicateName_ThrowsPluginRegistrationException()
		{
			_manager.AddPlugin(new GreetingPlugin(new FakeGreetingService()));

			Assert.Throws<PluginRegistrationException>(() =>
				_manager.AddPlugin(new GreetingPlugin(new FakeGreetingService())));
		}

		[Test]
		public void AddPlugin_NullPlugin_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => _manager.AddPlugin(null));
		}

		[Test]
		public void GetService_WithoutInstalledPlugin_FailsCommandWithClearMessage()
		{
			// No plugin installed: ctx.Greeting() throws PluginServiceNotFoundException, which
			// CommandDef.Execute converts into a failed (not crashed) command, same as any other exception.
			_manager.RegisterCommands(Assembly.GetExecutingAssembly());

			_manager.ExecuteCommand(nameof(GreetingCommands.Greet), new GreetArgs { Name = "Nobody" });

			Assert.IsTrue(_logger.Logs.Exists(l => l.Contains("No plugin service of type")));
		}

		[Test]
		public void InstalledPlugins_ListsInstalledPluginNames()
		{
			_manager.AddPlugin(new GreetingPlugin(new FakeGreetingService()));

			CollectionAssert.Contains(_manager.InstalledPlugins, "Greeting");
		}
	}
}
