using System;
using System.Linq;
using BlueCheese.CommandSync.Core;
using BlueCheese.CommandSync.Sample.Data;

namespace BlueCheese.CommandSync.Sample.Commands
{
	public static class PlayerCommands
	{
		/// <summary>
		/// "Logs in" as PlayerId with no authentication: creates a fresh guest profile the first time an Id
		/// is seen, or just bumps the login counter for a returning one. Safe to call every time the sample
		/// UI's Login button is pressed, including to switch to a different Id on the same device.
		/// </summary>
		[Command]
		public static void Login(CommandContext context, LoginArgs args)
		{
			if (string.IsNullOrWhiteSpace(args.PlayerId))
			{
				context.State.Fail("PlayerId cannot be empty.");
				return;
			}

			context.Data.Update((ref PlayersData data) =>
			{
				var players = (data.Players ?? Array.Empty<PlayerProfile>()).ToList();
				int index = players.FindIndex(p => p.PlayerId == args.PlayerId);
				if (index < 0)
				{
					// First time this Id is seen: give it a guest name. The numeric suffix comes from the
					// deterministic per-command RNG, so client and server always agree on the generated
					// name when this command is replayed.
					string guestName = $"Guest{context.RNG.Next(10000, 99999)}";
					players.Add(new PlayerProfile
					{
						PlayerId = args.PlayerId,
						PlayerName = guestName,
						XP = 0,
						LoginCount = 1
					});
				}
				else
				{
					var profile = players[index];
					profile.LoginCount += 1;
					players[index] = profile;
				}
				data.Players = players.ToArray();
			});
		}

		[Command]
		public static void SetPlayerName(CommandContext context, SetPlayerNameArgs args)
		{
			if (string.IsNullOrWhiteSpace(args.PlayerName))
			{
				context.State.Fail("Player name cannot be empty.");
				return;
			}

			var current = context.Data.Get<PlayersData>();
			if (!current.TryFind(args.PlayerId, out _))
			{
				context.State.Fail($"Unknown player '{args.PlayerId}'. Login first.");
				return;
			}

			context.Data.Update((ref PlayersData data) =>
			{
				var players = data.Players.ToList();
				int index = players.FindIndex(p => p.PlayerId == args.PlayerId);
				var profile = players[index];
				profile.PlayerName = args.PlayerName.Trim();
				players[index] = profile;
				data.Players = players.ToArray();
			});
		}

		[Command]
		public static void AddXP(CommandContext context, AddXPArgs args)
		{
			var current = context.Data.Get<PlayersData>();
			if (!current.TryFind(args.PlayerId, out _))
			{
				context.State.Fail($"Unknown player '{args.PlayerId}'. Login first.");
				return;
			}

			context.Data.Update((ref PlayersData data) =>
			{
				var players = data.Players.ToList();
				int index = players.FindIndex(p => p.PlayerId == args.PlayerId);
				var profile = players[index];
				profile.XP = Math.Max(0, profile.XP + args.Amount);
				players[index] = profile;
				data.Players = players.ToArray();
			});
		}
	}
}
