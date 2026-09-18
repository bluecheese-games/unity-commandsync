using System.Text;
using BlueCheese.CommandSync.Sample.Data;
using BlueCheese.CommandSync.Sample.Leaderboard;
using Newtonsoft.Json;

// Read-model refresh: pushes the current CommandSync state into the UI text elements built in
// CommandSyncSampleController.UI.cs. Called after every action that might have changed something.
public partial class CommandSyncSampleController
{
	private void RefreshProfileView()
	{
		if (string.IsNullOrEmpty(_currentPlayerId) || _dataManager == null)
		{
			_profileInfoText.text = "Not logged in.\nEnter a Player Id above and click Login.";
			return;
		}

		if (!_dataManager.Get<PlayersData>().TryFind(_currentPlayerId, out var profile))
		{
			_profileInfoText.text = $"Logging in as '{_currentPlayerId}'...";
			return;
		}

		int level = 1 + (int)(profile.XP / 100);
		_profileInfoText.text =
			$"Id: {profile.PlayerId}\n" +
			$"Name: {profile.PlayerName}\n" +
			$"XP: {profile.XP} (Level {level})\n" +
			$"Logins on this device: {profile.LoginCount}";
	}

	private void RefreshLeaderboardView()
	{
		if (_commandManager == null)
		{
			_leaderboardText.text = "-";
			_myRankText.text = string.Empty;
			return;
		}

		var leaderboard = _commandManager.GetService<ILeaderboardService>();
		var topScores = leaderboard.GetTopScores(XpLeaderboardId, LeaderboardTopCount);
		var playersData = _dataManager.Get<PlayersData>();

		var sb = new StringBuilder();
		for (int i = 0; i < topScores.Count; i++)
		{
			var entry = topScores[i];
			string name = playersData.TryFind(entry.PlayerId, out var player) ? player.PlayerName : entry.PlayerId;
			sb.AppendLine($"{i + 1}. {name} - {entry.Score} XP");
		}
		_leaderboardText.text = sb.Length > 0 ? sb.ToString() : "No scores yet.";

		if (string.IsNullOrEmpty(_currentPlayerId))
		{
			_myRankText.text = string.Empty;
			return;
		}

		var allScores = leaderboard.GetTopScores(XpLeaderboardId, int.MaxValue);
		int rankIndex = -1;
		for (int i = 0; i < allScores.Count; i++)
		{
			if (allScores[i].PlayerId == _currentPlayerId)
			{
				rankIndex = i;
				break;
			}
		}
		_myRankText.text = rankIndex >= 0
			? $"My rank: #{rankIndex + 1} of {allScores.Count}"
			: "My rank: not on the board yet - submit your score!";
	}

	private void Dump()
	{
		var playersData = _dataManager.Get<PlayersData>();
		var leaderboardsData = _dataManager.Get<LeaderboardsData>();
		var dumpObj = new
		{
			PlayersData = playersData,
			LeaderboardsData = leaderboardsData
		};
		string jsonData = JsonConvert.SerializeObject(dumpObj, Formatting.Indented);
		_debugText.text = jsonData;
	}
}
