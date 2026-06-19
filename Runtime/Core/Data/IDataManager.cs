//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;

namespace BlueCheese.CommandSync.Core
{

	public interface IDataManager : IReadOnlyDataManager
	{
		/// <summary>
		/// Sets the data of type T to the provided value.
		/// </summary>
		void Set<T>(T data);

		/// <summary>
		/// Flushes any pending changes to the underlying storage.
		/// Returns a list of types that were flushed, which can be used to trigger any necessary updates in the system for those types.
		/// </summary>
		IEnumerable<Type> Flush();

		/// <summary>
		/// Calculates a hash representing the current state of the data for the specified types. This can be used to detect changes in the data without needing to compare the actual values.
		/// </summary>
		long GetStateHash(IEnumerable<Type> keysToHash = null);

		/// <summary>
		/// Exports the requested data types into a serialized dictionary format for network transfer.
		/// </summary>
		Dictionary<string, string> ExportState(IEnumerable<Type> types);

		/// <summary>
		/// Imports a full state dictionary from the server, overwriting local storage and clearing the cache.
		/// </summary>
		void ImportState(Dictionary<string, string> serializedState);
	}
}