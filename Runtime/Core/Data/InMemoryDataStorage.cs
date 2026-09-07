using System;
using System.Collections.Generic;

namespace BlueCheese.CommandSync.Core
{
	public class InMemoryDataStorage : IDataStorage
	{
		private readonly Dictionary<string, object> _storage = new();

		public void Save<T>(string key, T data)
		{
			_storage[key] = data;
		}

		public bool TryLoad<T>(string key, out T data)
		{
			if (_storage.TryGetValue(key, out var obj) && obj is T typedObj)
			{
				data = typedObj;
				return true;
			}
			data = default;
			return false;
		}

		public void Save(string key, object data, Type type)
		{
			_storage[key] = data;
		}

		public bool TryLoad(string key, Type type, out object data)
		{
			if (_storage.TryGetValue(key, out var obj) && type.IsInstanceOfType(obj))
			{
				data = obj;
				return true;
			}
			data = null;
			return false;
		}
	}
}
