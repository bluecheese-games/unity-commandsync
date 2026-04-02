//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace BlueCheese.LocalCommands.Core
{
	public class NewtonsoftJsonSerializer : ISerializer
	{
		private readonly JsonSerializerSettings _settings;

		public NewtonsoftJsonSerializer(JsonSerializerSettings settings = null)
		{
			if (settings == null)
			{
				settings = new JsonSerializerSettings
				{
					TypeNameHandling = TypeNameHandling.Auto,
					Formatting = Formatting.None,
					NullValueHandling = NullValueHandling.Ignore,
					SerializationBinder = new SimpleNameBinder()
				};
			}

			_settings = settings;
		}

		public string Serialize<T>(T data) => JsonConvert.SerializeObject(data, _settings);

		public T Deserialize<T>(string serializedData) => JsonConvert.DeserializeObject<T>(serializedData, _settings);

		public string Serialize(object data, Type type) => JsonConvert.SerializeObject(data, type, _settings);

		public object Deserialize(string serializedData, Type type) => JsonConvert.DeserializeObject(serializedData, type, _settings);

		public class SimpleNameBinder : ISerializationBinder
		{
			public void BindToName(Type serializedType, out string assemblyName, out string typeName)
			{
				assemblyName = null;
				typeName = serializedType.FullName;
			}

			public Type BindToType(string assemblyName, string typeName)
			{
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					var type = assembly.GetType(typeName);
					if (type != null) return type;
				}
				return null;
			}
		}
	}
}
