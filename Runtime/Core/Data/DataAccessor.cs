using System.Collections.Generic;

namespace BlueCheese.CommandSync.Core
{
	public readonly struct DataAccessor
	{
		private readonly IInternalDataManager _dataManager;
		private readonly ExecutionState _executionState;

		internal DataAccessor(IInternalDataManager dataManager, ExecutionState executionState)
		{
			_dataManager = dataManager;
			_executionState = executionState;
		}

		/// <summary>
		/// Gets a read-only copy of the data of type T.
		/// Modifying this data will not affect the stored data.
		/// </summary>
		public readonly T Get<T>() where T : struct => _dataManager.Get<T>();

		/// <summary>
		/// Updates the data of type T by passing it to the provided action.
		/// The action can modify the data, and the changes will be saved back to the data manager.
		/// </summary>
		public void Update<T>(RefAction<T> action) where T : struct
		{
			// Get a reference to the data of type T.
			var box = _dataManager.GetBox<T>();
			var oldValue = box.Value;

			// Perform the update action, which can modify the value inside the box.
			action(ref box.Value);

			// If the value was modified, save the updated value back to the data manager and mark that data has been updated.
			// We use EqualityComparer<T>.Default to check for changes, which works for both value types and reference types (including null checks for reference types).
			// Consider using IEquatable<T> on the data types if possible, as that can provide a more efficient equality check.
			if (!EqualityComparer<T>.Default.Equals(oldValue, box.Value))
			{
				box.IsDirty = true; // Mark the box as dirty so that it will be saved during the next flush.
				_executionState.DataHasBeenUpdated = true;
			}
		}

		public delegate void RefAction<T>(ref T arg);
	}
}
