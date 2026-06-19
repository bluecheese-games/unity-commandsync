//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.LocalCommands.Core
{
	[Serializable]
	public class SyncRequest
	{
		public CommandCall[] Commands { get; set; }

		public long ClientStateHash { get; set; }
	}
}
