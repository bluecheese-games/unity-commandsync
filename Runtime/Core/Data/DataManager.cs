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
	public class DataManager : IInternalDataManager
	{
		private readonly Dictionary<Type, object> _cache = new();
		private readonly IDataStorage _storage;
		private readonly ISerializer _serializer;

		public IDataStorage Storage => _storage;

		public DataManager(IDataStorage storage, ISerializer serializer)
		{
			_storage = storage;
			_serializer = serializer;
		}

		public DataBox<T> GetBox<T>(T defaultValue = default)
		{
			var type = typeof(T);

			if (_cache.TryGetValue(type, out var obj) && obj is DataBox<T> box)
			{
				return box;
			}

			var value = Get(defaultValue);
			var newBox = new DataBox<T> { Value = value };
			_cache[type] = newBox;
			return newBox;
		}

		public T Get<T>(T defaultValue = default)
		{
			// Return the in-flight cached value so reads observe writes made earlier in the same command.
			if (_cache.TryGetValue(typeof(T), out var cached) && cached is DataBox<T> box)
			{
				return box.Value;
			}

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

		public void RevertChanges()
		{
			// Drop every dirty box so the next access reloads the last committed value from storage.
			var dirtyTypes = new List<Type>();
			foreach (var kvp in _cache)
			{
				if (kvp.Value is IDataBox box && box.IsDirty)
				{
					dirtyTypes.Add(kvp.Key);
				}
			}

			foreach (var type in dirtyTypes)
			{
				_cache.Remove(type);
			}
		}

		public long GetStateHash(IEnumerable<Type> keysToHash = null)
		{
			// Sort by FullName so the hash is deterministic regardless of Dictionary or caller order.
			var keys = (keysToHash ?? _cache.Keys).OrderBy(t => t.FullName);

			long hash = 17;
			foreach (var type in keys)
			{
				if (_storage.TryLoad(type.FullName, type, out object value))
				{
					var serialized = _serializer.Serialize(value);
					hash = hash * 31 + HashUtility.GetDeterministicHashCode64(serialized);
				}
				else
				{
					hash *= 31;
				}
			}
			return hash;
		}

		public Dictionary<string, string> ExportState(IEnumerable<Type> types)
		{
			var export = new Dictionary<string, string>();
			foreach (var type in types)
			{
				if (_cache.TryGetValue(type, out var obj) && obj is IDataBox box && box.IsDirty)
				{
					_storage.Save(type.FullName, box.UntypedValue);
					box.IsDirty = false;
				}

				if (_storage.TryLoad(type.FullName, type, out object value))
				{
					export[type.FullName] = _serializer.Serialize(value);
				}
			}
			return export;
		}

		public void ImportState(Dictionary<string, string> serializedState)
		{
			foreach (var kvp in serializedState)
			{
				Type type = TypeResolver.Resolve(kvp.Key);

				if (type == null)
				{
					throw new InvalidOperationException(
						$"ImportState failed: cannot resolve type '{kvp.Key}'. " +
						"The type may have been renamed, removed, or its assembly is not loaded.");
				}

				var deserialized = _serializer.Deserialize(kvp.Value, type);
				_storage.Save(kvp.Key, deserialized, type);
				_cache.Remove(type);
			}
		}
	}
}
