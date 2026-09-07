using BlueCheese.CommandSync.Core;

namespace BlueCheese.CommandSync.Sample.Leaderboard
{
	// Wraps CommandContext.GetService behind a named method so ILeaderboardService shows up in
	// auto-completion (ctx.Leaderboard()) as soon as this namespace is imported.
	public static class LeaderboardContextExtensions
	{
		public static ILeaderboardService Leaderboard(this CommandContext ctx) => ctx.GetService<ILeaderboardService>();
	}
}
