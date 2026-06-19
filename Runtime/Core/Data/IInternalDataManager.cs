//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.CommandSync.Core
{
	// Extends IDataManager with GetBox<T> for direct mutable access.
	// Kept internal so command authors cannot bypass dirty-tracking via the public interface.
	internal interface IInternalDataManager : IDataManager
	{
		DataBox<T> GetBox<T>(T defaultValue = default);
		IDataStorage Storage { get; }

		/// <summary>
		/// Discards all uncommitted (dirty) changes, restoring the last flushed state.
		/// Used to roll back the in-memory mutations of a command that failed.
		/// </summary>
		void RevertChanges();
	}
}
