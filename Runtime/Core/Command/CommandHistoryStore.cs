//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;

namespace BlueCheese.CommandSync.Core
{
	// Owns the persisted command history (queue of calls + set of updated data types) and its storage I/O.
	internal sealed class CommandHistoryStore
	{
		private const string CommandHistoryPrefKey = "CommandSync_History";

		private readonly IDataStorage _storage;
		private readonly CommandHistory _history;

		public CommandHistoryStore(IDataStorage storage)
		{
			_storage = storage;
			_history = Load();
		}

		public IDataStorage Storage => _storage;

		public int Count => _history.Queue.Count;

		public bool IsEmpty => _history.Queue.IsEmpty;

		public HashSet<Type> UpdatedData => _history.UpdatedData;

		public CommandCall[] ToArray() => _history.ToArray();

		public void Enqueue(CommandCall call) => _history.Enqueue(call);

		public void AddUpdatedTypes(IEnumerable<Type> updatedDataTypes)
		{
			foreach (var type in updatedDataTypes)
			{
				_history.UpdatedData.Add(type);
			}
		}

		public void Save() => _storage.Save(CommandHistoryPrefKey, _history, typeof(CommandHistory));

		public void Clear()
		{
			_history.Clear();
			Save();
		}

		private CommandHistory Load()
		{
			if (!_storage.TryLoad(CommandHistoryPrefKey, typeof(CommandHistory), out var historyObj) || historyObj == null)
			{
				return new CommandHistory();
			}
			return (CommandHistory)historyObj;
		}
	}
}
