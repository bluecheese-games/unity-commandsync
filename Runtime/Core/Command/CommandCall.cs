//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.LocalCommands.Core
{
	[Serializable]
	public record CommandCall
	{
		public Guid Id;
		public long Timestamp;
		public string ConfigVersion;
		public string CommandName;
		public object Args;

		public override string ToString()
		{
			var dt = DateTimeOffset.FromUnixTimeMilliseconds(Timestamp);
			string argsString = Args != null ? Args.ToString() : "null";
			return $"[{dt:yyyy-MM-dd HH:mm:ss}] {CommandName} ({argsString}) - Id: {Id}";
		}
	}
}
