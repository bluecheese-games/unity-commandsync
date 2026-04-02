//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.LocalCommands.Core
{
	public interface IDataStorage
	{
		void Save<T>(string key, T data);

		bool TryLoad<T>(string key, out T data);

		void Save(string key, object data, Type type);

		bool TryLoad(string key, Type type, out object data);
	}
}
