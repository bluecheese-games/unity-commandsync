//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.CommandSync.Core
{
	public interface IReadOnlyDataManager
	{
		/// <summary>
		/// Gets a copy of the data of type T. Modifying this data will not affect the stored data.
		/// If no data of type T exists, returns the default value for that type.
		/// </summary>
		T Get<T>(T defaultValue = default);
	}
}
