//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.LocalCommands.Core;
using BlueCheese.LocalCommands.Sample.Commands;
using BlueCheese.LocalCommands.Sample.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Profiling;

public class UnityLogger : BlueCheese.LocalCommands.Core.ILogger
{
	public void Log(string message) => Debug.Log(message);

	public void LogWarning(string message) => Debug.LogWarning(message);

	public void LogError(string message) => Debug.LogError(message);

	public void LogException(Exception exception) => Debug.LogException(exception);
}

public class PlayerPrefsDataStorage : IDataStorage
{
	private readonly ISerializer _serializer;
	private readonly string _prefix;

	public PlayerPrefsDataStorage(ISerializer serializer, string prefix)
	{
		_serializer = serializer;
		_prefix = prefix ?? "k";
	}

	public void Save<T>(string key, T data)
	{
		string serializedData = _serializer.Serialize(data);
		string fullKey = $"{_prefix}_{key}";
		PlayerPrefs.SetString(fullKey, serializedData);
	}

	public bool TryLoad<T>(string key, out T data)
	{
		string fullKey = $"{_prefix}_{key}";
		if (PlayerPrefs.HasKey(fullKey))
		{
			string serializedData = PlayerPrefs.GetString(fullKey);
			data = _serializer.Deserialize<T>(serializedData);
			return true;
		}
		data = default;
		return false;
	}

	public void Save(string key, object data, Type type)
	{
		string serializedData = _serializer.Serialize(data, type);
		string fullKey = $"{_prefix}_{key}";
		PlayerPrefs.SetString(fullKey, serializedData);
	}

	public bool TryLoad(string key, Type type, out object data)
	{
		string fullKey = $"{_prefix}_{key}";
		if (PlayerPrefs.HasKey(fullKey))
		{
			string serializedData = PlayerPrefs.GetString(fullKey);
			data = _serializer.Deserialize(serializedData, type);
			return true;
		}
		data = null;
		return false;
	}
}

public class DummyCommandSyncService : ISyncService
{
	public Task<SyncResponse> SyncAsync(SyncRequest request)
	{
		Debug.Log($"Syncing {request.Commands.Length} commands to server...");
		foreach (var cmd in request.Commands)
		{
			Debug.Log($" - {cmd}");
		}

		// Simulate a successful sync with no conflicts
		return Task.FromResult(SyncResponse.Ok());
	}

	public Task<FetchResponse> FetchAsync()
	{
		// Simulate fetching full state from server (empty in this dummy implementation)
		return Task.FromResult(FetchResponse.Ok(new()));
	}
}

public class LocalCommandsSampleController : MonoBehaviour
{
	[SerializeField] private Transform _buttonsContainer;
	[SerializeField] private GameObject _buttonPrefab;
	[SerializeField] private TextMeshProUGUI _debugText;
	[SerializeField] private string _syncEndpoint = "https://localhost:7259/sync";

	private LocalCommandManager _localCommandManager;
	private IReadOnlyDataManager _dataManager;

	private async void Awake()
	{
		await InitializeManager();
	}

	private async Task InitializeManager()
	{
		var logger = new UnityLogger();
		var serializer = new NewtonsoftJsonSerializer();
		var dataStorage = new PlayerPrefsDataStorage(serializer, "Sample");
		var dataManager = new DataManager(dataStorage, serializer);
		var timeProvider = new TimeProvider();
		var syncService = new UnityHttpSyncService(_syncEndpoint, serializer);
		var commandsDataStorage = new PlayerPrefsDataStorage(serializer, "Commands");
		var config = Config.Create();
		_localCommandManager = new LocalCommandManager(dataManager, logger, config, timeProvider, commandsDataStorage, syncService);
		_localCommandManager.RegisterCommands(typeof(SampleCommands).Assembly);

		_dataManager = dataManager;

		await _localCommandManager.LoadData();

		Dump();
	}

	private void Dump()
	{
		var sampleData = _dataManager.Get<SampleData>();
		var sampleData2 = _dataManager.Get<SampleData2>();
		var dumpObj = new
		{
			SampleData = sampleData,
			SampleData2 = sampleData2
		};
		string jsonData = JsonConvert.SerializeObject(dumpObj, Formatting.Indented);
		_debugText.text = jsonData;
	}

	private void AddValue()
	{
		Profiler.BeginSample(nameof(AddValue));
		_localCommandManager.ExecuteCommand(SampleCommands.AddValue, new SampleCommandArgs { Value = 10 });
		Profiler.EndSample();

		Dump();
	}

	private void AddValueByName()
	{
		Profiler.BeginSample(nameof(AddValueByName));
		_localCommandManager.ExecuteCommand(nameof(SampleCommands.AddValue), new SampleCommandArgs { Value = 10 });
		Profiler.EndSample();

		Dump();
	}

	private void ResetValue()
	{
		Profiler.BeginSample(nameof(ResetValue));
		_localCommandManager.ExecuteCommand(SampleCommands.RESET_COMMAND_NAME);
		Profiler.EndSample();

		Dump();
	}

	private void ResetAndAddValue()
	{
		Profiler.BeginSample(nameof(ResetAndAddValue));
		_localCommandManager.ExecuteCommand(SampleCommands.ResetAndAddValue, new SampleCommandArgs { Value = 20 });
		Profiler.EndSample();

		Dump();
	}

	private void AddValueMultipleTimes()
	{
		Profiler.BeginSample(nameof(AddValueMultipleTimes));
		_localCommandManager.ExecuteCommand(SampleCommands.AddValueMultipleTimes, new SampleCommandArgs { Value = 5 });
		Profiler.EndSample();

		Dump();
	}

	private void NestedWriteValue()
	{
		Profiler.BeginSample(nameof(NestedWriteValue));
		_localCommandManager.ExecuteCommand(SampleCommands.NestedWriteValue);
		Profiler.EndSample();

		Dump();
	}

	private void AddFloatValue()
	{
		Profiler.BeginSample(nameof(AddFloatValue));
		_localCommandManager.ExecuteCommand(SampleCommands.AddFloatValue, new SampleCommandArgs { FloatValue = 1000000.5f });
		Profiler.EndSample();
		Dump();
	}

	private void AddRandomDoubleValue()
	{
		Profiler.BeginSample(nameof(AddRandomDoubleValue));
		_localCommandManager.ExecuteCommand(SampleCommands.AddRandomDoubleValue);
		Profiler.EndSample();
		Dump();
	}

	private void LogValue()
	{
		Profiler.BeginSample(nameof(LogValue));
		_localCommandManager.ExecuteCommand(SampleCommands.LogValue);
		Profiler.EndSample();

		Dump();
	}

	private void FailingCommand()
	{
		Profiler.BeginSample(nameof(FailingCommand));
		_localCommandManager.ExecuteCommand(SampleCommands.FailingCommand);
		Profiler.EndSample();

		Dump();
	}

	private void FailingCommandWithException()
	{
		Profiler.BeginSample(nameof(FailingCommandWithException));
		_localCommandManager.ExecuteCommand(SampleCommands.FailingCommandWithException);
		Profiler.EndSample();

		Dump();
	}

	private void SetText()
	{
		Profiler.BeginSample(nameof(SetText));
		_localCommandManager.ExecuteCommand(SampleCommands.SetText, new TextCommandArgs { Text = "Current time: " + DateTime.Now.ToString("T") });
		Profiler.EndSample();

		Dump();
	}

	private async Task Sync()
	{
		await _localCommandManager.Sync();
	}

	private void Reset()
	{
		PlayerPrefs.DeleteAll();
		_localCommandManager.ClearHistory();
		Dump();
	}

	private void Start()
	{
		CreateButton("Add Value", AddValue);
		CreateButton("Add Value By Name", AddValueByName);
		CreateButton("Reset Value", ResetValue);
		CreateButton("Reset and Add Value", ResetAndAddValue);
		CreateButton("Add Value Multiple Times", AddValueMultipleTimes);
		CreateButton("Nested Write Value", NestedWriteValue);
		CreateButton("Add Float Value", AddFloatValue);
		CreateButton("Add Random Double Value", AddRandomDoubleValue);
		CreateButton("Log Value", LogValue);
		CreateButton("Failing Command", FailingCommand);
		CreateButton("Failing Command With Exception", FailingCommandWithException);
		CreateButton("Set Text", SetText);
		CreateButton("Sync (Clear Queue)", Sync);
		CreateButton("Reset (Clear History & PlayerPrefs)", Reset);
	}

	private void CreateButton(string label, Action onClick)
	{
		var buttonObj = Instantiate(_buttonPrefab, _buttonsContainer);
		var button = buttonObj.GetComponent<UnityEngine.UI.Button>();
		var text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
		text.text = label;
		button.onClick.AddListener(() => onClick());
	}

	private void CreateButton(string label, Func<Task> onClickAsync)
	{
		var buttonObj = Instantiate(_buttonPrefab, _buttonsContainer);
		var button = buttonObj.GetComponent<UnityEngine.UI.Button>();
		var text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
		text.text = label;
		button.onClick.AddListener(() => _ = onClickAsync());
	}

	/// <summary>
	/// Synchronization service that sends the command history to an HTTP API.
	/// </summary>
	public class UnityHttpSyncService : ISyncService
	{
		private readonly string _serverUrl;
		private readonly ISerializer _serializer;

		public UnityHttpSyncService(string serverUrl, ISerializer serializer)
		{
			_serverUrl = serverUrl;
			_serializer = serializer;
		}

		public async Task<SyncResponse> SyncAsync(SyncRequest request)
		{
			// Serialize the request (Commands + Hash)
			string jsonPayload = _serializer.Serialize(request);

			// Create a UnityWebRequest for POSTing the JSON payload to the server
			using var www = new UnityWebRequest(_serverUrl, "POST");
			byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
			www.uploadHandler = new UploadHandlerRaw(bodyRaw);
			www.downloadHandler = new DownloadHandlerBuffer();
			www.SetRequestHeader("Content-Type", "application/json");

			// Send the request and get an operation handle
			var operation = www.SendWebRequest();

			// Asynchronously wait for the web request to complete
			while (!operation.isDone)
			{
				await Task.Delay(10); // Avoids blocking the main Unity thread
			}

			// Network error handling
			if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
			{
				Debug.LogError($"[SyncService] Sync error: {www.error}");
				return SyncResponse.Fail(www.error);
			}

			try
			{
				// Deserialize the server response
				var responseText = www.downloadHandler.text;
				var response = JsonConvert.DeserializeObject<SyncResponse>(responseText);

				Debug.Log($"[SyncService] Server response: Success: {response.Success} with message: {response.Message}");
				return response ?? SyncResponse.Fail("Failed to deserialize server response.");
			}
			catch (Exception e)
			{
				Debug.LogError($"[SyncService] Failed to read server response: {e.Message}");
				return SyncResponse.Fail(e.Message);
			}
		}

		public async Task<FetchResponse> FetchAsync()
		{
			// Derives the state URL by replacing /sync with /state
			string stateUrl = _serverUrl.Replace("/sync", "/state");

			using var www = UnityWebRequest.Get(stateUrl);
			var operation = www.SendWebRequest();

			while (!operation.isDone)
			{
				await Task.Delay(10);
			}

			if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
			{
				Debug.LogError($"[SyncService] Fetch state error: {www.error}");
				return FetchResponse.Fail(www.error);
			}

			try
			{
				var responseText = www.downloadHandler.text;
				// Deserialize the dictionary returned by the StateController
				var response = _serializer.Deserialize<FetchResponse>(responseText);
				return response ?? FetchResponse.Fail("Failed to deserialize server response.");
			}
			catch (Exception e)
			{
				Debug.LogError($"[SyncService] Failed to parse state from server: {e.Message}");
				return FetchResponse.Fail(e.Message);
			}
		}
	}
}