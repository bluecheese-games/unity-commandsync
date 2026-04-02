//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.LocalCommands.Core
{
	public interface IConfig
	{
		T Get<T>(string key, T defaultValue = default);
	}
}
