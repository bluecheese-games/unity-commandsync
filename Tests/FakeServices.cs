using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlueCheese.CommandSync.Core;

namespace BlueCheese.CommandSync.Tests
{
	public class FakeDataStorage : IDataStorage
	{
		public Dictionary<string, object> Storage = new();
		public bool SaveCalled = false;

		public void Save<T>(string key, T data)
		{
			Storage[key] = data;
			SaveCalled = true;
		}

		public bool TryLoad<T>(string key, out T data)
		{
			if (Storage.TryGetValue(key, out var obj) && obj is T typedObj)
			{
				data = typedObj;
				return true;
			}
			data = default;
			return false;
		}

		public void Save(string key, object data, Type type) => Save(key, data);

		public bool TryLoad(string key, Type type, out object data)
		{
			if (Storage.TryGetValue(key, out var obj) && type.IsInstanceOfType(obj))
			{
				data = obj;
				return true;
			}
			data = null;
			return false;
		}
	}

	public class FakeLogger : ILogger
	{
		public List<string> Logs = new();
		public void Log(string message) => Logs.Add(message);
		public void LogWarning(string message) => Logs.Add("WARN: " + message);
		public void LogError(string message) => Logs.Add("ERR: " + message);
		public void LogException(Exception ex) => Logs.Add("EX: " + ex.Message);
	}

	public class FakeCommandSyncService : ISyncService
	{
		public Task<FetchResponse> FetchAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(FetchResponse.Ok(new()));
		}

		public Task<SyncResponse> SyncAsync(SyncRequest request, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(SyncResponse.Ok());
		}
	}
}
