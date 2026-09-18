using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlueCheese.CommandSync.Core;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

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

	public async Task<SyncResponse> SyncAsync(SyncRequest request, CancellationToken cancellationToken = default)
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
			cancellationToken.ThrowIfCancellationRequested();
			await Task.Delay(10, cancellationToken); // Avoids blocking the main Unity thread
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

	public async Task<FetchResponse> FetchAsync(CancellationToken cancellationToken = default)
	{
		// Derives the state URL by replacing /sync with /state
		string stateUrl = _serverUrl.Replace("/sync", "/state");

		using var www = UnityWebRequest.Get(stateUrl);
		var operation = www.SendWebRequest();

		while (!operation.isDone)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await Task.Delay(10, cancellationToken);
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
