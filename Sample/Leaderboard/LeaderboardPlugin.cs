using BlueCheese.CommandSync.Core;

namespace BlueCheese.CommandSync.Sample.Leaderboard
{
	/// <summary>
	/// Reference plugin: adds one or more leaderboards (identified by a string Id, see <see cref="SubmitScoreArgs.LeaderboardId"/>)
	/// to a CommandManager without touching Core. Install with <c>commandManager.AddPlugin(new LeaderboardPlugin())</c>.
	/// </summary>
	public sealed class LeaderboardPlugin : IPlugin
	{
		public const string MaxEntriesConfigKey = "Leaderboard.MaxEntries";
		public const int DefaultMaxEntries = 100;

		public string Name => "Leaderboard";

		public void Install(PluginInstallContext context)
		{
			context.Services.Add<ILeaderboardService>(new LeaderboardService(context.Data));
		}
	}
}
