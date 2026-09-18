using System;

namespace BlueCheese.CommandSync.Sample.Data
{
	// Players is always replaced wholesale (never mutated in place) when a profile is added or updated,
	// because DataAccessor.Update detects changes via IEquatable<T>, and arrays don't compare by content
	// (same caveat as LeaderboardsData in Sample/Leaderboard).
	[Serializable]
	public struct PlayersData : IEquatable<PlayersData>
	{
		public PlayerProfile[] Players;

		public readonly bool Equals(PlayersData other) => ReferenceEquals(Players, other.Players);
	}
}
