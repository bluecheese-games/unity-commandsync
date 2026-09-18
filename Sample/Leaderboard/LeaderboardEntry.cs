using System;

namespace BlueCheese.CommandSync.Sample.Leaderboard
{
	[Serializable]
	public struct LeaderboardEntry
	{
		public string PlayerId;
		public long Score;
	}
}
