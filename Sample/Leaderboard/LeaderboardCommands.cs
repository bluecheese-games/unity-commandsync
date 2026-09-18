using System;
using System.Linq;
using BlueCheese.CommandSync.Core;

namespace BlueCheese.CommandSync.Sample.Leaderboard
{
	public static class LeaderboardCommands
	{
		[Command]
		public static void SubmitScore(CommandContext context, SubmitScoreArgs args)
		{
			int maxEntries = context.Config.Get(LeaderboardPlugin.MaxEntriesConfigKey, LeaderboardPlugin.DefaultMaxEntries);

			context.Data.Update((ref LeaderboardsData data) =>
			{
				var boards = (data.Boards ?? Array.Empty<LeaderboardBoard>()).ToList();
				int boardIndex = boards.FindIndex(b => b.Id == args.LeaderboardId);
				var entries = boardIndex >= 0 ? boards[boardIndex].Entries : null;
				entries ??= Array.Empty<LeaderboardEntry>();

				long bestScore = Math.Max(args.Score, entries
					.Where(e => e.PlayerId == args.PlayerId)
					.Select(e => e.Score)
					.DefaultIfEmpty(long.MinValue)
					.Max());

				var updatedEntries = entries
					.Where(e => e.PlayerId != args.PlayerId)
					.Append(new LeaderboardEntry { PlayerId = args.PlayerId, Score = bestScore })
					// Deterministic order (score desc, PlayerId as tie-break) so client and server always
					// compute the same ranking and therefore the same state hash.
					.OrderByDescending(e => e.Score)
					.ThenBy(e => e.PlayerId, StringComparer.Ordinal)
					.Take(maxEntries)
					.ToArray();

				var board = new LeaderboardBoard { Id = args.LeaderboardId, Entries = updatedEntries };
				if (boardIndex >= 0)
				{
					boards[boardIndex] = board;
				}
				else
				{
					boards.Add(board);
				}

				data.Boards = boards.ToArray(); // new array reference: required for the change to be detected (see LeaderboardsData.Equals)
			});
		}

		[Command]
		public static void ClearLeaderboard(CommandContext context, ClearLeaderboardArgs args)
		{
			context.Data.Update((ref LeaderboardsData data) =>
			{
				var boards = (data.Boards ?? Array.Empty<LeaderboardBoard>()).ToList();
				int boardIndex = boards.FindIndex(b => b.Id == args.LeaderboardId);
				if (boardIndex < 0 || boards[boardIndex].Entries == null || boards[boardIndex].Entries.Length == 0)
				{
					return; // already empty: leave data.Boards untouched so this is correctly a no-op, not a wasted history entry
				}

				boards[boardIndex] = new LeaderboardBoard { Id = args.LeaderboardId, Entries = Array.Empty<LeaderboardEntry>() };
				data.Boards = boards.ToArray();
			});
		}
	}
}
