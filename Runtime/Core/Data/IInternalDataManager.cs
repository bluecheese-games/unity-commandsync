//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.LocalCommands.Core
{
	// Extends IDataManager with GetBox<T> for direct mutable access.
	// Kept internal so command authors cannot bypass dirty-tracking via the public interface.
	internal interface IInternalDataManager : IDataManager
	{
		DataBox<T> GetBox<T>(T defaultValue = default);
		IDataStorage Storage { get; }
	}
}
