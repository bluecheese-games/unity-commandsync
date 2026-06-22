//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

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
	}
}
