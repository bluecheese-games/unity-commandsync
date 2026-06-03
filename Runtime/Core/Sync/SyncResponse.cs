//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.LocalCommands.Core
{
	[Serializable]
	public class SyncResponse : Response
	{
		static public new SyncResponse Ok() => new()
		{
			Success = true
		};

		static public new SyncResponse Fail(string errorMessage) => new()
		{
			Success = false,
			Message = errorMessage
		};
	}
}
