using System.Collections.Generic;

namespace BlueCheese.CommandSync.Core
{
	public class Config : IConfig
	{
		private const string DefaultVersion = "1.0.0";

		private readonly Dictionary<string, object> _config = new();
		private readonly string _version;

		public static Config Create(IDictionary<string, object> values = null, string version = null)
		{
			return new Config(values, version);
		}

		public Config() { }

		internal Config(IDictionary<string, object> values, string version = null)
		{
			if (values != null)
			{
				foreach (var kvp in values)
				{
					_config[kvp.Key] = kvp.Value;
				}
			}

			_version = version;
		}

		public string Version => _version ?? DefaultVersion;

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
