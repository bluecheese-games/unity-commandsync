//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.LocalCommands.Core
{
	public class Response
	{
		public bool Success { get; set; }
		public string Message { get; set; }
		public static Response Ok() => new() { Success = true, Message = "Ok" };
		public static Response Fail(string message = null) => new() { Success = false, Message = message };
	}
}
