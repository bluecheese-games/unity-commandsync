using System;

namespace BlueCheese.CommandSync.Sample.Leaderboard
{
	[Serializable]
	public struct LeaderboardBoard
	{
		public string Id;
		public LeaderboardEntry[] Entries;
	}
}
