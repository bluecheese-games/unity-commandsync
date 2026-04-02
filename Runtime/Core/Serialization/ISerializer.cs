//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.LocalCommands.Core
{
	public interface ISerializer
	{
		string Serialize<T>(T data);

		T Deserialize<T>(string serializedData);

		string Serialize(object data, Type type);

		object Deserialize(string serializedData, Type type);
	}
}
