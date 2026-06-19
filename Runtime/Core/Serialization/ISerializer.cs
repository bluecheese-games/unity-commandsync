//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.CommandSync.Core
{
	public interface ISerializer
	{
		string Serialize<T>(T data);

		T Deserialize<T>(string serializedData);

		string Serialize(object data, Type type);

		object Deserialize(string serializedData, Type type);

		/// <summary>
		/// Converts a serialization intermediate value (e.g. a JObject produced when a loosely-typed
		/// field is deserialized) into the requested target type.
		/// Returns false and leaves <paramref name="result"/> untouched when the value is not a
		/// convertible intermediate, so callers can fall back to their own type validation.
		/// </summary>
		bool TryConvert(object value, Type targetType, out object result);
	}
}
