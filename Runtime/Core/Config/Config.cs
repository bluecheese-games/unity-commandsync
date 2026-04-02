//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System.Collections.Generic;

namespace BlueCheese.LocalCommands.Core
{
	public class Config : IConfig
	{
		private readonly Dictionary<string, object> _config = new();

		public static Config Create(IDictionary<string, object> values = null)
		{
			return new Config(values);
		}

		public Config() { }

		internal Config(IDictionary<string, object> values)
		{
			if (values != null)
			{
				foreach (var kvp in values)
				{
					_config[kvp.Key] = kvp.Value;
				}
			}
		}

		public T Get<T>(string key, T defaultValue = default)
		{
			if (_config.TryGetValue(key, out var value) && value is T typedValue)
			{
				return typedValue;
			}
			return defaultValue;
		}
	}
}
