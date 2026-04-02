//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.LocalCommands.Core
{
	[Serializable]
	public class SyncRequest
	{
		public LocalCommandManager.CommandCall[] Commands { get; set; }

		public int ClientStateHash { get; set; }
	}
}
