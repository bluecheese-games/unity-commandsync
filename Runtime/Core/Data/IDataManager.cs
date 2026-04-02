//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;

namespace BlueCheese.LocalCommands.Core
{

	public interface IDataManager : IReadOnlyDataManager
	{
		/// <summary>
		/// Sets the data of type T to the provided value.
		/// </summary>
		void Set<T>(T data);

		/// <summary>
		/// Gets a reference to the data of type T wrapped in a DataBox. Modifying the Value property of the returned DataBox will update the stored data.
		/// </summary>
		DataBox<T> GetBox<T>(T defaultValue = default);

		/// <summary>
		/// Flushes any pending changes to the underlying storage.
		/// Returns a list of types that were flushed, which can be used to trigger any necessary updates in the system for those types.
		/// </summary>
		IEnumerable<Type> Flush();

		/// <summary>
		/// Calculates a hash representing the current state of the data for the specified types. This can be used to detect changes in the data without needing to compare the actual values.
		/// </summary>
		int GetStateHash(IEnumerable<Type> keysToHash = null);
	}
}
