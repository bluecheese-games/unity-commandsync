//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.LocalCommands.Core
{
	[Serializable]
	public class SyncResponse
	{
		public SyncResult Result { get; set; }

		public string Message { get; set; }

		public static SyncResponse Success() => new() { Result = SyncResult.Success, Message = "Ok" };
		public static SyncResponse Desync() => new() { Result = SyncResult.Desync, Message = "Desynchronized" };
		public static SyncResponse Error(string message = null) => new() { Result = SyncResult.Error, Message = message };
	}
}
