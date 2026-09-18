using System.Threading.Tasks;
using BlueCheese.CommandSync.Sample.Commands;
using BlueCheese.CommandSync.Sample.Data;
using BlueCheese.CommandSync.Sample.Leaderboard;
using UnityEngine;

// Button click handlers.
public partial class CommandSyncSampleController
{
	private void OnLoginClicked()
	{
		string playerId = (_loginIdInput.text ?? string.Empty).Trim();
		if (string.IsNullOrEmpty(playerId))
		{
			Debug.Log("[Sample] Enter a Player Id first.");
			return;
		}

		_commandManager.ExecuteCommand(nameof(PlayerCommands.Login), new LoginArgs { PlayerId = playerId });
		_currentPlayerId = playerId;
		_loginIdInput.text = string.Empty;

		RefreshProfileView();
		RefreshLeaderboardView();
		Dump();
	}

	private void OnSaveNameClicked()
	{
		if (!EnsureLoggedIn()) return;

		string newName = (_playerNameInput.text ?? string.Empty).Trim();
		if (string.IsNullOrEmpty(newName))
		{
			Debug.Log("[Sample] Enter a new display name first.");
			return;
		}

		_commandManager.ExecuteCommand(nameof(PlayerCommands.SetPlayerName),
			new SetPlayerNameArgs { PlayerId = _currentPlayerId, PlayerName = newName });
		_playerNameInput.text = string.Empty;

		RefreshProfileView();
		Dump();
	}

	private void OnAddXpClicked(long amount)
	{
		if (!EnsureLoggedIn()) return;

		_commandManager.ExecuteCommand(nameof(PlayerCommands.AddXP), new AddXPArgs { PlayerId = _currentPlayerId, Amount = amount });

		RefreshProfileView();
		Dump();
	}

	private void OnSubmitScoreClicked()
	{
		if (!EnsureLoggedIn()) return;

		if (!_dataManager.Get<PlayersData>().TryFind(_currentPlayerId, out var profile))
		{
			Debug.Log("[Sample] No profile data yet for this player.");
			return;
		}

		_commandManager.ExecuteCommand(nameof(LeaderboardCommands.SubmitScore),
			new SubmitScoreArgs { LeaderboardId = XpLeaderboardId, PlayerId = _currentPlayerId, Score = profile.XP });

		RefreshLeaderboardView();
		Dump();
	}

	private void OnClearLeaderboardClicked()
	{
		// Not gated by EnsureLoggedIn(): clearing the board is a global action, not tied to any one player.
		_commandManager.ExecuteCommand(nameof(LeaderboardCommands.ClearLeaderboard),
			new ClearLeaderboardArgs { LeaderboardId = XpLeaderboardId });

		RefreshLeaderboardView();
		Dump();
	}

	private bool EnsureLoggedIn()
	{
		if (string.IsNullOrEmpty(_currentPlayerId))
		{
			Debug.Log("[Sample] Login first.");
			return false;
		}
		return true;
	}

	private async Task Sync()
	{
		await _commandManager.Sync();
	}

	// Wipes only the client's local storage (its data + pending command queue) and rebuilds the client
	// manager against it, still talking to the SAME mock server. Since the rebuilt manager starts with an
	// empty history, CommandManager.LoadData() treats this like a fresh install and fetches the full
	// authoritative state back down from the server — so this is closer to "reinstall the app" than to
	// "go offline forever".
	private async Task ClearClientData()
	{
		_clientDataStorage.ClearAll();
		_clientCommandsStorage.ClearAll();
		_currentPlayerId = null;

		await CreateClientManager();

		RefreshProfileView();
		RefreshLeaderboardView();
		Dump();
	}

	// Wipes client AND server storage, then rebuilds both from scratch exactly once. Clearing both before
	// rebuilding (rather than composing two "clear one side, keep the other" calls) matters: if the client
	// were rebuilt first while the server still had its old data, CommandManager.LoadData()'s empty-history
	// Fetch would immediately re-download that old data into the "cleared" client, making the reset appear
	// to do nothing.
	private async Task ResetAll()
	{
		_clientDataStorage.ClearAll();
		_clientCommandsStorage.ClearAll();
		_mockServerDataStorage.ClearAll();
		_currentPlayerId = null;

		_mockServer = CreateMockServer(_mockServerDataStorage);
		await CreateClientManager();

		RefreshProfileView();
		RefreshLeaderboardView();
		Dump();
	}
}
