using System;

namespace BlueCheese.CommandSync.Sample.Leaderboard
{
	[Serializable]
	public struct LeaderboardEntry
	{
		public string PlayerId;
		public long Score;
	}

	[Serializable]
	public struct LeaderboardBoard
	{
		public string Id;
		public LeaderboardEntry[] Entries;
	}

	// Boards is always replaced wholesale (never mutated in place) when a score is submitted, because
	// DataAccessor.Update detects changes via IEquatable<T>, and arrays don't compare by content.
	[Serializable]
	public struct LeaderboardsData : IEquatable<LeaderboardsData>
	{
		public LeaderboardBoard[] Boards;

		public readonly bool Equals(LeaderboardsData other) => ReferenceEquals(Boards, other.Boards);
	}
}
