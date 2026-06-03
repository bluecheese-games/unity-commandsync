//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.LocalCommands.Core
{
	public interface IConfig
	{
		string Version { get; }

		T Get<T>(string key, T defaultValue = default);
	}
}
