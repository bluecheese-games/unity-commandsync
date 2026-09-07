using System.Collections.Generic;

namespace BlueCheese.CommandSync.Sample.Leaderboard
{
	public interface ILeaderboardService
	{
		/// <summary>
		/// Returns up to <paramref name="count"/> entries of the leaderboard identified by <paramref name="leaderboardId"/>,
		/// ordered by descending score. Returns an empty list if the leaderboard has no entries yet.
		/// </summary>
		IReadOnlyList<LeaderboardEntry> GetTopScores(string leaderboardId, int count = 10);
	}
}
