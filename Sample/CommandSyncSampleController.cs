using BlueCheese.CommandSync.Core;
using BlueCheese.CommandSync.Sample.Commands;
using BlueCheese.CommandSync.Sample.Leaderboard;
using BlueCheese.CommandSync.Sample.MockServer;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

/// <summary>
/// Player dashboard for the CommandSync sample: log in as any Id (no auth), view/edit that player's
/// profile (name, XP), and interact with the "xp" leaderboard (see Sample/Leaderboard). Everything is
/// built procedurally at runtime into the existing "Buttons" column of the sample scene, so the scene
/// asset itself never needs to be edited by hand.
///
/// Split across several files by responsibility (all part of this same partial class):
/// - CommandSyncSampleController.cs (this file): fields, lifecycle, manager bootstrap/rebuild.
/// - CommandSyncSampleController.UI.cs: procedural UI construction helpers.
/// - CommandSyncSampleController.Actions.cs: button click handlers.
/// - CommandSyncSampleController.Display.cs: read-model refresh (profile/leaderboard text, JSON dump).
/// </summary>
public partial class CommandSyncSampleController : MonoBehaviour
{
	[SerializeField] private Transform _buttonsContainer;
	[SerializeField] private GameObject _buttonPrefab;
	[SerializeField] private TextMeshProUGUI _debugText;
	[SerializeField] private string _syncEndpoint = "https://localhost:7259/sync";

	private const string XpLeaderboardId = "xp";
	private const int LeaderboardTopCount = 10;

	private CommandManager _commandManager;
	private IReadOnlyDataManager _dataManager;
	private string _currentPlayerId;

	private Transform _contentRoot;

	private ISerializer _serializer;
	private PlayerPrefsDataStorage _clientDataStorage;
	private PlayerPrefsDataStorage _clientCommandsStorage;
	private PlayerPrefsDataStorage _mockServerDataStorage;
	private MockCommandServer _mockServer;

	private TMP_InputField _loginIdInput;
	private TextMeshProUGUI _profileInfoText;
	private TMP_InputField _playerNameInput;
	private TextMeshProUGUI _leaderboardText;
	private TextMeshProUGUI _myRankText;

	private async void Awake()
	{
		// Build the UI synchronously first: Awake() always finishes (up to the first await) before
		// Start() runs, so this guarantees every UI element exists before InitializeManager()'s
		// continuation (which may resume after Start() has had a chance to run) touches them.
		SetupScrollableContent();
		BuildLoginSection();
		BuildProfileSection();
		BuildLeaderboardSection();
		BuildUtilitySection();

		await InitializeManager();
	}

	private async Task InitializeManager()
	{
		_serializer = new NewtonsoftJsonSerializer();

		// In-process mock backend that replays commands and reconciles by state hash (see MockServer assembly).
		// Its storage must persist across Play Mode restarts too (a separate PlayerPrefs prefix from the
		// client's own), or the "server" forgets everything on every restart while the client's local data
		// does not — which looks exactly like the server having reset, and reliably desyncs on the next sync.
		_mockServerDataStorage = new PlayerPrefsDataStorage(_serializer, "MockServerState");
		_mockServer = CreateMockServer(_mockServerDataStorage);

		_clientDataStorage = new PlayerPrefsDataStorage(_serializer, "Sample");
		_clientCommandsStorage = new PlayerPrefsDataStorage(_serializer, "Commands");
		await CreateClientManager();

		RefreshProfileView();
		RefreshLeaderboardView();
		Dump();
	}

	private MockCommandServer CreateMockServer(IDataStorage dataStorage)
	{
		var mockServer = new MockCommandServer(_serializer, dataStorage);
		mockServer.RegisterCommands(typeof(PlayerCommands).Assembly);
		mockServer.RegisterCommands(typeof(LeaderboardCommands).Assembly); // must replay score submissions too, or every Submit Score desyncs
		return mockServer;
	}

	// (Re)builds the client-side manager against _clientDataStorage/_clientCommandsStorage and wires it to
	// whatever _mockServer currently is. Called once at startup, and again after Clear Client Data / Clear
	// All, since neither CommandManager nor MockServerSyncService can be repointed after construction.
	private async Task CreateClientManager()
	{
		// Swap MockServerSyncService for UnityHttpSyncService(_syncEndpoint, serializer) to target a real server.
		var syncService = new MockServerSyncService(_mockServer, _serializer);
		var dataManager = new DataManager(_clientDataStorage, _serializer);
		_commandManager = new CommandManager(dataManager,
			logger: new UnityLogger(),
			config: Config.Create(),
			timeProvider: new SystemTimeProvider(),
			commandsDataStorage: _clientCommandsStorage,
			syncService: syncService);
		_commandManager.RegisterCommands(typeof(PlayerCommands).Assembly);
		_commandManager.AddPlugin(new LeaderboardPlugin()); // also auto-registers LeaderboardCommands on the client

		_dataManager = dataManager;

		await _commandManager.LoadData();
	}
}
