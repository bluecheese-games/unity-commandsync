using System;
using System.Collections.Generic;
using System.Linq;
using BlueCheese.CommandSync.Core;

namespace BlueCheese.CommandSync.Sample.Leaderboard
{
	// Read-only: score submission always goes through LeaderboardCommands.SubmitScore, so it is replayable
	// and participates in the client/server state hash. This service only reads the committed state.
	internal sealed class LeaderboardService : ILeaderboardService
	{
		private readonly IReadOnlyDataManager _dataManager;

		internal LeaderboardService(IReadOnlyDataManager dataManager)
		{
			_dataManager = dataManager;
		}

		public IReadOnlyList<LeaderboardEntry> GetTopScores(string leaderboardId, int count = 10)
		{
			var data = _dataManager.Get<LeaderboardsData>();
			if (data.Boards == null)
			{
				return Array.Empty<LeaderboardEntry>();
			}

			foreach (var board in data.Boards)
			{
				if (board.Id == leaderboardId)
				{
					return (board.Entries ?? Array.Empty<LeaderboardEntry>()).Take(count).ToArray();
				}
			}
			return Array.Empty<LeaderboardEntry>();
		}
	}
}
