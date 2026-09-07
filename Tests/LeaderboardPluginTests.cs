using System.Collections.Generic;
using System.Linq;
using BlueCheese.CommandSync.Core;
using BlueCheese.CommandSync.Sample.Leaderboard;
using NUnit.Framework;

namespace BlueCheese.CommandSync.Tests
{
	public struct LeaderboardReadData
	{
		public int TopScoresCountSeenFromCommand;
	}

	// Exercises ctx.Leaderboard() from within a command, in addition to CommandManager.GetService<T>() from outside one.
	public static class LeaderboardReadCommands
	{
		[Command]
		public static void ReadTopScoresIntoData(CommandContext context, SubmitScoreArgs args)
		{
			var topScores = context.Leaderboard().GetTopScores(args.LeaderboardId);
			context.Data.Update((ref LeaderboardReadData data) => data.TopScoresCountSeenFromCommand = topScores.Count);
		}
	}

	[TestFixture]
	public class LeaderboardPluginTests
	{
		private CommandManager _manager;
		private DataManager _dataManager;

		[SetUp]
		public void Setup()
		{
			var storage = new FakeDataStorage();
			_dataManager = new DataManager(storage, new NewtonsoftJsonSerializer());
			_manager = new CommandManager(_dataManager, new FakeLogger(), Config.Create(), new SystemTimeProvider(), storage);
			_manager.AddPlugin(new LeaderboardPlugin());
		}

		private void Submit(string leaderboardId, string playerId, long score) =>
			_manager.ExecuteCommand(nameof(LeaderboardCommands.SubmitScore),
				new SubmitScoreArgs { LeaderboardId = leaderboardId, PlayerId = playerId, Score = score });

		[Test]
		public void SubmitScore_ThenGetTopScores_ReturnsTheEntry()
		{
			Submit("weekly", "Ada", 100);

			var scores = _manager.GetService<ILeaderboardService>().GetTopScores("weekly");

			Assert.AreEqual(1, scores.Count);
			Assert.AreEqual("Ada", scores[0].PlayerId);
			Assert.AreEqual(100, scores[0].Score);
		}

		[Test]
		public void DifferentLeaderboardIds_AreIsolatedFromEachOther()
		{
			Submit("weekly", "Ada", 100);
			Submit("all-time", "Grace", 999);

			var service = _manager.GetService<ILeaderboardService>();

			Assert.AreEqual(1, service.GetTopScores("weekly").Count);
			Assert.AreEqual("Ada", service.GetTopScores("weekly")[0].PlayerId);
			Assert.AreEqual(1, service.GetTopScores("all-time").Count);
			Assert.AreEqual("Grace", service.GetTopScores("all-time")[0].PlayerId);
		}

		[Test]
		public void SubmitScore_LowerScoreForSamePlayer_DoesNotOverwriteBestScore()
		{
			Submit("weekly", "Ada", 100);
			Submit("weekly", "Ada", 50);

			var scores = _manager.GetService<ILeaderboardService>().GetTopScores("weekly");

			Assert.AreEqual(1, scores.Count);
			Assert.AreEqual(100, scores[0].Score);
		}

		[Test]
		public void SubmitScore_HigherScoreForSamePlayer_OverwritesBestScore()
		{
			Submit("weekly", "Ada", 100);
			Submit("weekly", "Ada", 150);

			var scores = _manager.GetService<ILeaderboardService>().GetTopScores("weekly");

			Assert.AreEqual(1, scores.Count);
			Assert.AreEqual(150, scores[0].Score);
		}

		[Test]
		public void GetTopScores_OrdersByDescendingScore_WithPlayerIdAsDeterministicTieBreak()
		{
			Submit("weekly", "Bob", 50);
			Submit("weekly", "Ada", 100);
			Submit("weekly", "Zoe", 50);

			var scores = _manager.GetService<ILeaderboardService>().GetTopScores("weekly");

			CollectionAssert.AreEqual(
				new[] { "Ada", "Bob", "Zoe" },
				scores.Select(s => s.PlayerId).ToArray(),
				"Ties must break on PlayerId so replaying the same commands always yields the same order.");
		}

		[Test]
		public void GetTopScores_UnknownLeaderboardId_ReturnsEmpty()
		{
			var scores = _manager.GetService<ILeaderboardService>().GetTopScores("does-not-exist");

			Assert.IsEmpty(scores);
		}

		[Test]
		public void SubmitScore_BeyondMaxEntries_TrimsLowestScores()
		{
			var config = Config.Create(new Dictionary<string, object> { { LeaderboardPlugin.MaxEntriesConfigKey, 2 } });
			var storage = new FakeDataStorage();
			_dataManager = new DataManager(storage, new NewtonsoftJsonSerializer());
			_manager = new CommandManager(_dataManager, new FakeLogger(), config, new SystemTimeProvider(), storage);
			_manager.AddPlugin(new LeaderboardPlugin());

			Submit("weekly", "Ada", 300);
			Submit("weekly", "Bob", 200);
			Submit("weekly", "Zoe", 100);

			var scores = _manager.GetService<ILeaderboardService>().GetTopScores("weekly");

			CollectionAssert.AreEqual(new[] { "Ada", "Bob" }, scores.Select(s => s.PlayerId).ToArray());
		}

		[Test]
		public void CommandContext_Leaderboard_ExtensionMethod_WorksInsideACommand()
		{
			_manager.RegisterCommands(System.Reflection.Assembly.GetExecutingAssembly());
			Submit("weekly", "Ada", 100);

			_manager.ExecuteCommand(nameof(LeaderboardReadCommands.ReadTopScoresIntoData),
				new SubmitScoreArgs { LeaderboardId = "weekly" });

			Assert.AreEqual(1, _dataManager.Get<LeaderboardReadData>().TopScoresCountSeenFromCommand);
		}
	}
}
