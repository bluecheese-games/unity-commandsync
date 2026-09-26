using BlueCheese.CommandSync.Core;
using BlueCheese.CommandSync.Sample.MockServer;
using NUnit.Framework;
using System.Reflection;
using System.Threading.Tasks;

namespace BlueCheese.CommandSync.Tests
{
	// End-to-end round-trip: client executes commands, syncs to the in-process MockServer (over serialized
	// JSON), which replays them and reconciles by state hash. Validates determinism, hashing, argument
	// reconstruction and the desync recovery path through the full pipeline.
	[TestFixture]
	public class MockServerRoundTripTests
	{
		private static CommandManager CreateClient(ISerializer serializer, MockCommandServer server, out DataManager clientData)
		{
			clientData = new DataManager(new FakeDataStorage(), serializer);
			var client = new CommandManager(clientData,
				syncService: new MockServerSyncService(server, serializer),
				serializer: serializer);
			client.RegisterCommands(Assembly.GetExecutingAssembly());
			return client;
		}

		[Test]
		public async Task Sync_WhenServerReplayMatches_ReturnsOkAndClearsHistory()
		{
			var serializer = new NewtonsoftJsonSerializer();
			var server = new MockCommandServer(serializer);
			server.RegisterCommands(Assembly.GetExecutingAssembly());

			var client = CreateClient(serializer, server, out _);

			client.ExecuteCommand(nameof(TestCommands.AddScore), new TestArgs { Value = 5 });
			await client.Sync();

			Assert.AreEqual(0, client.History.Count, "A matching sync must clear the client history.");
			Assert.AreEqual(5, server.GetState<TestScoreData>().Score,
				"The server must reach the same state by replaying the command.");
		}

		[Test]
		public async Task Sync_WhenServerStateDiverges_TriggersDesyncRecovery()
		{
			var serializer = new NewtonsoftJsonSerializer();
			var server = new MockCommandServer(serializer) { ForceDesync = true };
			server.RegisterCommands(Assembly.GetExecutingAssembly());

			var client = CreateClient(serializer, server, out var clientData);

			client.ExecuteCommand(nameof(TestCommands.AddScore), new TestArgs { Value = 5 });
			await client.Sync();

			// The server double-applied the command (score = 10), so the hash diverged from the client (5):
			// the client must detect the desync, re-fetch and adopt the authoritative server state.
			Assert.AreEqual(0, client.History.Count, "Desync recovery must clear the client history.");
			Assert.AreEqual(10, clientData.Get<TestScoreData>().Score,
				"After desync recovery the client must adopt the authoritative server state.");
		}

		[Test]
		public async Task Sync_WhenPluginAddedViaServerAddPlugin_ReplaysWithoutDesync()
		{
			// Only AddPlugin is called on both sides, no manual RegisterCommands(pluginAssembly) on the
			// server — proves MockCommandServer.AddPlugin keeps the server in lock-step with the client,
			// the same way CommandManager.AddPlugin already does for the client's own registry. Deliberately
			// does NOT use the CreateClient helper: it blanket-registers the whole test assembly, which
			// would double-register GreetingPlugin's commands once AddPlugin scans that same assembly.
			var serializer = new NewtonsoftJsonSerializer();
			var server = new MockCommandServer(serializer);
			server.AddPlugin(new GreetingPlugin(new FakeGreetingService()));

			var clientData = new DataManager(new FakeDataStorage(), serializer);
			var client = new CommandManager(clientData,
				syncService: new MockServerSyncService(server, serializer),
				serializer: serializer);
			client.AddPlugin(new GreetingPlugin(new FakeGreetingService()));

			client.ExecuteCommand(nameof(GreetingCommands.Greet), new GreetArgs { Name = "Grace" });
			await client.Sync();

			Assert.AreEqual(0, client.History.Count, "A matching sync must clear the client history.");
			Assert.AreEqual("Hello, Grace!", server.GetState<GreetingData>().LastGreeting,
				"The server must have replayed the plugin's command to reach the same state.");
		}
	}
}
