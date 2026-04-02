//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.LocalCommands.Core
{
	public class DataBox<T> : IDataBox
	{
		public T Value;

		public bool IsDirty { get; set; }
		object IDataBox.UntypedValue => Value;
	}
}
