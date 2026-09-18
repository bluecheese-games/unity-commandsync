using System;
using System.Collections.Generic;
using System.Linq;
using BlueCheese.CommandSync.Core;
using UnityEngine;

public class PlayerPrefsDataStorage : IDataStorage
{
	private readonly ISerializer _serializer;
	private readonly string _prefix;
	private readonly HashSet<string> _knownKeys;

	private string IndexKey => $"{_prefix}___index";

	public PlayerPrefsDataStorage(ISerializer serializer, string prefix)
	{
		_serializer = serializer;
		_prefix = prefix ?? "k";
		_knownKeys = LoadIndex();
	}

	private HashSet<string> LoadIndex()
	{
		if (PlayerPrefs.HasKey(IndexKey))
		{
			var keys = _serializer.Deserialize<string[]>(PlayerPrefs.GetString(IndexKey));
			return new HashSet<string>(keys ?? Array.Empty<string>());
		}
		return new HashSet<string>();
	}

	// Every fullKey ever written is tracked in its own PlayerPrefs entry (PlayerPrefs has no cross-platform
	// key enumeration API), so ClearAll() below can wipe exactly this instance's data — and nothing else
	// sharing the same PlayerPrefs store, unlike PlayerPrefs.DeleteAll().
	private void RememberKey(string fullKey)
	{
		if (_knownKeys.Add(fullKey))
		{
			PlayerPrefs.SetString(IndexKey, _serializer.Serialize(_knownKeys.ToArray()));
		}
	}

	public void Save<T>(string key, T data)
	{
		string serializedData = _serializer.Serialize(data);
		string fullKey = $"{_prefix}_{key}";
		PlayerPrefs.SetString(fullKey, serializedData);
		RememberKey(fullKey);
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
		RememberKey(fullKey);
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

	/// <summary>Deletes every key this instance (i.e. this prefix) has ever written, and only those.</summary>
	public void ClearAll()
	{
		foreach (var fullKey in _knownKeys)
		{
			PlayerPrefs.DeleteKey(fullKey);
		}
		_knownKeys.Clear();
		PlayerPrefs.DeleteKey(IndexKey);
	}
}
