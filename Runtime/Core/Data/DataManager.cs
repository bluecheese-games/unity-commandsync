//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueCheese.LocalCommands.Core
{
	/// <summary>
	/// Manages the storage and retrieval of data for local commands.
	/// It provides a way to get and set data of any type, and handles caching and flushing of data to the underlying storage.
	/// Data is stored in "boxes" that track whether they have been modified (dirty) and need to be saved back to storage.
	/// Data is identified by its type, and the storage uses the full name of the type as the key for saving and loading data.
	/// </summary>
	public class DataManager : IDataManager
	{
		private readonly Dictionary<Type, object> _cache = new();
		private readonly IDataStorage _storage;
		private readonly ISerializer _serializer;

		public DataManager(IDataStorage storage, ISerializer serializer)
		{
			_storage = storage;
			_serializer = serializer;
		}

		public DataBox<T> GetBox<T>(T defaultValue = default)
		{
			var type = typeof(T);

			// Check if we already have a cached box for this type. If so, return it.
			if (_cache.TryGetValue(type, out var obj) && obj is DataBox<T> box)
			{
				return box;
			}

			// If not, we need to load the data from storage (or use the default value if it doesn't exist) and create a new box for it.
			var value = Get(defaultValue);
			var newBox = new DataBox<T> { Value = value };
			_cache[type] = newBox;
			return newBox;
		}

		public T Get<T>(T defaultValue = default)
		{
			if (_storage.TryLoad(typeof(T).FullName, out T storedValue))
			{
				return storedValue;
			}
			return defaultValue;
		}

		public void Set<T>(T data)
		{
			var box = GetBox<T>();
			box.Value = data;
			box.IsDirty = true;
		}

		public IEnumerable<Type> Flush()
		{
			foreach (var kvp in _cache)
			{
				if (kvp.Value is IDataBox box && box.IsDirty)
				{
					_storage.Save(kvp.Key.FullName, box.UntypedValue);
					box.IsDirty = false;
					yield return kvp.Key;
				}
			}
		}

		public int GetStateHash(IEnumerable<Type> keysToHash = null)
		{
			if (keysToHash == null || !keysToHash.Any())
			{
				keysToHash = _cache.Keys;
			}

			int hash = 17;
			foreach (var type in keysToHash)
			{
				if (_storage.TryLoad(type.FullName, type, out object value))
				{
					var serialized = _serializer.Serialize(value);
					hash = hash * 31 + HashUtility.GetDeterministicHashCode(serialized);
				}
				else
				{
					hash = hash * 31; // No value, just multiply by prime to change hash
				}
			}
			return hash;
		}
	}
}
