//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.CommandSync.Core
{
	public interface IDataBox
	{
		bool IsDirty { get; set; }
		object UntypedValue { get; }
	}
}
